using FlowNode.node.Attribute;

namespace FlowNode.node
{
    /// <summary>
    /// 打印节点：将输入的值写入执行日志面板，便于在 GUI 中观察执行结果。
    /// Value 引脚为 object 类型，可接受任意数据引脚。
    /// </summary>
    [SystemNode("Debug/Print")]
    public class PrintNode : NodeBase
    {
        private Pin pin_input;
        private Pin pin_output;
        private Pin pin_value;

        public PrintNode()
        {
            Name = "Print";
        }

        public override void allocateDefaultPins()
        {
            pin_input = createPin("Input", PinDirection.Input, PinType.Execute);
            pin_output = createPin("Output", PinDirection.Output, PinType.Execute);
            pin_value = createPin("Value", PinDirection.Input, PinType.Data, typeof(object), null);
        }

        public override void excute(INodeManager manager)
        {
            var value = pin_value.data;
            manager.WriteLog("[Print] " + (value != null ? value.ToString() : "null"));
            manager.pushNextConnectNode(pin_output);
        }
    }
}
