using FlowNode.node.Attribute;

namespace FlowNode.node
{
    /// <summary>
    /// 注释节点：执行流透传，Text 属性可在属性面板编辑作说明（不参与运算）。
    /// </summary>
    [SystemNode("Debug/Comment")]
    public class CommentNode : NodeBase
    {
        private Pin pin_input;
        private Pin pin_output;

        /// <summary>注释文本，可在属性面板编辑。</summary>
        public string Text { get; set; } = string.Empty;

        public CommentNode()
        {
            Name = "Comment";
        }

        public override void allocateDefaultPins()
        {
            pin_input = createPin("Input", PinDirection.Input, PinType.Execute);
            pin_output = createPin("Output", PinDirection.Output, PinType.Execute);
        }

        public override void excute(INodeManager manager)
        {
            manager.pushNextConnectNode(pin_output);
        }

        public override string GetDisplaySubtitle()
        {
            return string.IsNullOrEmpty(Text) ? null : Text;
        }
    }
}
