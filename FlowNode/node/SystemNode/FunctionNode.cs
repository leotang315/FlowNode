using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FlowNode1.node
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
            pin_input = createPin("Input", PinDirection.Input, new PinType());
            pin_output = createPin("Output", PinDirection.Input, new PinType());

            var parameters = method.GetParameters().ToList();
            foreach (var param in parameters)
            {
                createPin(param.Name, param.IsOut ? PinDirection.Output : PinDirection.Input, PinType.Data, param.ParameterType, null);
            }
        }

        public override void excute(INodeManager manager)
        {
            var parameters = Pins.Select(p => p.data).ToArray();
            method.Invoke(self, parameters);

            for (int i = 0; i < parameters.Length; i++)
            {
                if (Pins[i].direction == PinDirection.Output)
                {
                    Pins[i].data = parameters[i];
                }
            }
            // 将下一个节点推入执行堆栈
            manager.pushNextConnectNode(pin_output);
        }
    }
}
