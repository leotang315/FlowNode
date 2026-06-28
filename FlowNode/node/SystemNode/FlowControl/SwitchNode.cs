using System;

namespace FlowNode.node
{
    /// <summary>
    /// 按整型 Index 路由到 Case0..Case(N-1)，否则走 Default。CaseCount 为 1~4。
    /// </summary>
    [SystemNode("Switch")]
    public class SwitchNode : NodeBase
    {
        public const int MaxCases = 4;

        /// <summary>有效分支数（1~4）。引脚固定为 4 路 Case + Default。</summary>
        public int CaseCount { get; set; } = 2;

        public Pin pin_input;
        public Pin pin_index;
        public Pin pin_default;
        public Pin pin_case0;
        public Pin pin_case1;
        public Pin pin_case2;
        public Pin pin_case3;

        public SwitchNode()
        {
            Name = "Switch";
        }

        public override void allocateDefaultPins()
        {
            pin_input = createPin("Input", PinDirection.Input, PinType.Execute);
            pin_index = createPin("Index", PinDirection.Input, PinType.Data, typeof(int), 0);
            pin_case0 = createPin("Case0", PinDirection.Output, PinType.Execute);
            pin_case1 = createPin("Case1", PinDirection.Output, PinType.Execute);
            pin_case2 = createPin("Case2", PinDirection.Output, PinType.Execute);
            pin_case3 = createPin("Case3", PinDirection.Output, PinType.Execute);
            pin_default = createPin("Default", PinDirection.Output, PinType.Execute);
        }

        public override void excute(INodeManager manager)
        {
            int index = pin_index.data is int i ? i : 0;
            int count = Math.Max(1, Math.Min(MaxCases, CaseCount));

            Pin target;
            if (index >= 0 && index < count)
            {
                switch (index)
                {
                    case 0: target = pin_case0; break;
                    case 1: target = pin_case1; break;
                    case 2: target = pin_case2; break;
                    default: target = pin_case3; break;
                }
            }
            else
            {
                target = pin_default;
            }

            manager.pushNextConnectNode(target);
        }

        public override string GetDisplaySubtitle()
        {
            int index = pin_index?.data is int i ? i : 0;
            int count = Math.Max(1, Math.Min(MaxCases, CaseCount));
            if (index >= 0 && index < count)
                return "→Case" + index;
            return "→Default";
        }
    }
}
