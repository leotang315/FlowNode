using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FlowNode.node
{
    public class FunctionNode : NodeBase
    {
        public object self;
        public MethodInfo method { get; set; }
        public Pin pin_input;
        public Pin pin_output;

        public FunctionNode(object self, MethodInfo method)
        {
            this.self = self;
            this.method = method;
        }
        public override void allocateDefaultPins()
        {
            if (!IsAutoRun)
            {
                pin_input = createPin("Input", PinDirection.Input, new PinType());
                pin_output = createPin("Output", PinDirection.Output, new PinType());
            }


            var parameters = method.GetParameters().ToList();
            foreach (var param in parameters)
            {
                createPin(param.Name, param.IsOut ? PinDirection.Output : PinDirection.Input, PinType.Data, param.ParameterType, null);
            }
        }

        public override void excute(INodeManager manager)
        {
            var dataPins = Pins.Where(p => p.pinType == PinType.Data).ToList();

            // 只选择数据类型的Pin
            var parameters = dataPins.Select(p => p.data).ToArray();
            method.Invoke(self, parameters);

            // 更新输出参数

            for (int i = 0; i < parameters.Length; i++)
            {
                if (dataPins[i].direction == PinDirection.Output)
                {
                    dataPins[i].data = parameters[i];
                }
            }

            if (!IsAutoRun)
            {
                // 将下一个节点推入执行堆栈
                manager.pushNextConnectNode(pin_output);
            }
        }
    }
}
