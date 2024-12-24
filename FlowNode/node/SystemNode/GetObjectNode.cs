using System;
using System.Linq;
namespace FlowNode.node
{
    public class GetObjectNode : NodeBase
    {
        private string m_objectName;

        public GetObjectNode(string key)
        {
            m_objectName = key;
            IsAutoRun = true;
        }

        public override void allocateDefaultPins()
        {
            createPin(m_objectName, PinDirection.Output, PinType.Data, typeof(object), null);
        }

        public override void excute(INodeManager manager)
        {
            var outputPin = Pins.FirstOrDefault(p => p.Name == m_objectName);
            if (outputPin != null)
            {
                outputPin.data = manager.GetDataObject(m_objectName);
            }
        }
    }
} 