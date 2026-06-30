using System;
using System.Linq;
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

        /// <summary>绑定的全局变量名（复制粘贴与序列化识别用）。</summary>
        public string VariableName => m_objectName;

        /// <summary>绑定的全局变量类型。</summary>
        public Type VariableType => m_objectType;

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