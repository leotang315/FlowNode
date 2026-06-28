using System.Threading;
using FlowNode.node.Attribute;

namespace FlowNode.node
{
    /// <summary>
    /// 延时节点：在执行流中暂停指定毫秒后继续（会阻塞当前执行线程）。
    /// </summary>
    [SystemNode("Debug/Delay")]
    public class DelayNode : NodeBase
    {
        private Pin pin_input;
        private Pin pin_output;

        /// <summary>延时毫秒数，可在属性面板编辑。</summary>
        public int DelayMs { get; set; } = 100;

        public DelayNode()
        {
            Name = "Delay";
        }

        public override void allocateDefaultPins()
        {
            pin_input = createPin("Input", PinDirection.Input, PinType.Execute);
            pin_output = createPin("Output", PinDirection.Output, PinType.Execute);
        }

        public override void excute(INodeManager manager)
        {
            if (DelayMs > 0)
            {
                Thread.Sleep(DelayMs);
            }
            manager.pushNextConnectNode(pin_output);
        }

        public override string GetDisplaySubtitle()
        {
            return DelayMs + "ms";
        }
    }
}
