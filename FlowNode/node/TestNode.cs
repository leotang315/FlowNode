using FlowNode.node.Attribute;
using FlowNode.node;
namespace FlowNode.node
{
    [SystemNode("Test/TestNode")]
    public class TestNode : NodeBase
    {
        public override void allocateDefaultPins()
        {
            // 添加输入输出引脚
            createPin("In", PinDirection.Input, PinType.Execute);
            createPin("Out", PinDirection.Output, PinType.Execute);
        }

        public override void excute(INodeManager manager)
        {
            // 实现节点的执行逻辑
        }
    }
} 