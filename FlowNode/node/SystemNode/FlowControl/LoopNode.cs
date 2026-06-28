using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowNode.node
{
    [SystemNode("Loop")]
    public class LoopNode : NodeBase
    {
        /// <summary>循环次数。作为公有属性以便在属性面板编辑并参与序列化。</summary>
        public int LoopCount { get; set; } = 1;

        // 内部迭代计数器，与对外的 Index 输出引脚分离，循环结束后归零以支持多次运行。
        private int currentIndex;

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
            if (currentIndex < LoopCount)
            {
                // 暴露当前迭代序号（0 基）供循环体读取
                pin_index.data = currentIndex;
                currentIndex++;

                // 先压回自身（循环体执行完后再次回到这里），再压入循环体；
                // 栈为 LIFO，故循环体先执行，随后回到本节点继续下一轮判断
                manager.pushNextNode(this);
                manager.pushNextConnectNode(pin_loopBody);
            }
            else
            {
                // 循环结束：归零计数器以支持下次运行，并走 Completed 分支
                currentIndex = 0;
                manager.pushNextConnectNode(pin_completed);
            }
        }

        public override string GetDisplaySubtitle()
        {
            return "×" + LoopCount;
        }
    }

}
