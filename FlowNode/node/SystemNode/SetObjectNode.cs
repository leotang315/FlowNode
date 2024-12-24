using System;
using System.Linq;
namespace FlowNode.node
{
    public class SetObjectNode : NodeBase
    {
        private string m_objectName;

        public SetObjectNode(string key)
        {
            m_objectName = key;
        }

        public override void allocateDefaultPins()
        {
            createPin("Input", PinDirection.Input, PinType.Execute);
            createPin("Output", PinDirection.Output, PinType.Execute);
            createPin(m_objectName, PinDirection.Input, PinType.Data, typeof(object), null);
        }

        public override void excute(INodeManager manager)
        {
            var valuePin = Pins.FirstOrDefault(p => p.Name == m_objectName);
            if (valuePin != null)
            {
                manager.SetDataObject(m_objectName, valuePin.data);
            }

            // 执行下一个节点
            manager.pushNextConnectNode(Pins.FirstOrDefault(p => p.Name == "Output"));
        }
    }
} 