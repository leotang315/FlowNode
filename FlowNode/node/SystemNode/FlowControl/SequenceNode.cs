using FlowNode.node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowNode
{
    [SystemNode("Sequence")]
    public class SequenceNode : NodeBase
    {
        public Pin pin_input;
        public Pin pin_output;
        public SequenceNode()
        {
        }
        public override void excute(INodeManager manager)
        {


            doWork();

            // 将下一个节点推入执行堆栈
            manager.pushNextConnectNode(pin_output);
        }
        public virtual void doWork()
        {

        }

        public override void allocateDefaultPins()
        {
            pin_input = createPin("Input", PinDirection.Input, new PinType());
            pin_output = createPin("Output", PinDirection.Input, new PinType());
        }
    }
}
