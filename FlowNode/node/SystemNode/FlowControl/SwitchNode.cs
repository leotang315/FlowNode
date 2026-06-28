using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FlowNode.node
{
    /// <summary>
    /// 按整型 Index 路由到 Case0..Case(N-1)，否则走 Default。CaseCount 为 1~32，变更时同步引脚数量。
    /// </summary>
    [SystemNode("Switch")]
    public class SwitchNode : NodeBase
    {
        public const int MaxCases = 32;

        /// <summary>有效分支数（1~32）。变更后调用 <see cref="SyncCasePins"/> 增删 Case 引脚。</summary>
        public int CaseCount { get; set; } = 2;

        public Pin pin_input;
        public Pin pin_index;
        public Pin pin_default;

        public SwitchNode()
        {
            Name = "Switch";
        }

        public int GetEffectiveCaseCount()
        {
            return Math.Max(1, Math.Min(MaxCases, CaseCount));
        }

        public override void allocateDefaultPins()
        {
            pin_input = createPin("Input", PinDirection.Input, PinType.Execute);
            pin_index = createPin("Index", PinDirection.Input, PinType.Data, typeof(int), 0);
            pin_default = createPin("Default", PinDirection.Output, PinType.Execute);
            SyncCasePins(null);
        }

        /// <summary>按 <see cref="CaseCount"/> 增删 Case 输出引脚；缩小时移除多余引脚上的连线。</summary>
        public void SyncCasePins(NodeManager manager)
        {
            int target = GetEffectiveCaseCount();
            var casePins = GetCasePins();

            while (casePins.Count > target)
            {
                var pin = casePins[casePins.Count - 1];
                RemovePinAndConnectors(manager, pin);
                casePins.RemoveAt(casePins.Count - 1);
            }

            while (casePins.Count < target)
            {
                AddCasePin(casePins.Count);
                casePins = GetCasePins();
            }
        }

        public override void excute(INodeManager manager)
        {
            int index = pin_index.data is int i ? i : 0;
            int count = GetEffectiveCaseCount();

            Pin target;
            if (index >= 0 && index < count)
            {
                target = findPin("Case" + index) ?? pin_default;
            }
            else
            {
                target = pin_default;
            }

            if (target != null)
                manager.pushNextConnectNode(target);
        }

        public override string GetDisplaySubtitle()
        {
            int index = pin_index?.data is int i ? i : 0;
            int count = GetEffectiveCaseCount();
            if (index >= 0 && index < count)
                return count + "路 →Case" + index;
            return count + "路 →Default";
        }

        private List<Pin> GetCasePins()
        {
            return Pins
                .Where(IsCasePin)
                .OrderBy(p => CaseIndexFromName(p.Name))
                .ToList();
        }

        private Pin AddCasePin(int index)
        {
            var pin = new Pin(this)
            {
                Name = "Case" + index,
                direction = PinDirection.Output,
                pinType = PinType.Execute
            };

            int insertAt = Pins.Count;
            if (pin_default != null)
            {
                int defaultIndex = Pins.IndexOf(pin_default);
                if (defaultIndex >= 0)
                    insertAt = defaultIndex;
            }

            Pins.Insert(insertAt, pin);
            return pin;
        }

        private static void RemovePinAndConnectors(NodeManager manager, Pin pin)
        {
            if (manager != null)
            {
                foreach (var connector in manager.getConnectors()
                    .Where(c => c.src == pin || c.dst == pin)
                    .ToList())
                {
                    manager.removeConnector(connector);
                }
            }

            pin.host.Pins.Remove(pin);
        }

        private static bool IsCasePin(Pin pin)
        {
            if (pin.direction != PinDirection.Output || pin.pinType != PinType.Execute)
                return false;
            if (!pin.Name.StartsWith("Case", StringComparison.Ordinal) || pin.Name.Length <= 4)
                return false;

            return int.TryParse(pin.Name.Substring(4), NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
        }

        private static int CaseIndexFromName(string name)
        {
            return int.TryParse(name.Substring(4), NumberStyles.Integer, CultureInfo.InvariantCulture, out int index)
                ? index
                : 0;
        }
    }
}
