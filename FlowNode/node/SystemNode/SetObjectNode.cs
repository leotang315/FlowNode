using System;
using System.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
namespace FlowNode.node
{
    public class SetObjectNode : NodeBase
    {
        private string m_objectName;
        private Type m_objectType;
        private Pin m_pin_output;
        private Pin m_pin_value;

        public SetObjectNode(string varName, Type varType)
        {
            Name = "Set " + varName;
            m_objectName = varName;
            m_objectType = varType;
        }

        public override void allocateDefaultPins()
        {
            createPin("Input", PinDirection.Input, PinType.Execute);
            m_pin_output = createPin("Output", PinDirection.Output, PinType.Execute);
            m_pin_value = createPin(m_objectName, PinDirection.Input, PinType.Data, m_objectType, null);
        }

        public override void excute(INodeManager manager)
        {
            manager.SetDataObject(m_objectName, m_pin_value.data, m_objectType);

            // 执行下一个节点
            manager.pushNextConnectNode(m_pin_output);
        }
    }
}