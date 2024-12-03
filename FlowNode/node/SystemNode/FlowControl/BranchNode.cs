using FlowNode.node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowNode
{
    [SystemNode("Branch")]
    public class BranchNode : NodeBase
    {
        public Pin pin_input;
        public Pin pin_true;
        public Pin pin_false;
        public Pin pin_condition;

        public BranchNode()
        {
        }

        public override void allocateDefaultPins()
        {
            pin_input = createPin("Input", PinDirection.Input, PinType.Execute);
            pin_true = createPin("True", PinDirection.Output, PinType.Execute);
            pin_false = createPin("False", PinDirection.Output, PinType.Execute);
            pin_condition = createPin("Condition", PinDirection.Input, PinType.Data);
        }

        public override void excute(INodeManager manager)
        {
            // 根据分支条件将下一个节点推入执行堆栈
            if ((Boolean)pin_condition.data)
            {
                manager.pushNextConnectNode(pin_true);
            }
            else
            {
                manager.pushNextConnectNode(pin_false);
            }
        }
    }
}
