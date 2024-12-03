using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowNode1.node
{
    [SystemNode("Loop")]
    public class LoopNode : NodeBase
    {
        public int loopCount = 1;

        public Pin pin_input;
        public Pin pin_loopBody;
        public Pin pin_completed;
        public Pin pin_index;

        public LoopNode()
        {
        }

        public override void allocateDefaultPins()
        {
            pin_input = createPin("Input", PinDirection.Input, PinType.Execute);
            pin_loopBody = createPin("LoopBody", PinDirection.Output, PinType.Execute);
            pin_completed = createPin("Completed", PinDirection.Output, PinType.Execute);
            pin_index = createPin("Index", PinDirection.Output, PinType.Data, typeof(int), 0);
        }

        public override void excute(INodeManager manager)
        {
            if ((int)pin_index.data < loopCount)
            {
                // 将循环体压入执行堆栈
                manager.pushNextNode(this);
                pin_index.data = (int)pin_index.data + 1;
                manager.pushNextConnectNode(pin_loopBody);
            }
            else
            {
                // 循环结束,执行下一个节点
                manager.pushNextConnectNode(pin_completed);
            }
        }
    }

}
