using System;
using System.Linq;
using System.Xml.Linq;
namespace FlowNode.node
{
    public class GetObjectNode : NodeBase
    {
        private string m_objectName;
        private Type m_objectType;
        private Pin m_pin_value;

        public GetObjectNode(string varName, Type varType)
        {
            Name="Get "+ varName;
            m_objectName = varName;
            m_objectType = varType;
            IsAutoRun = true;
        }

        /// <summary>绑定的全局变量名（复制粘贴与序列化识别用）。</summary>
        public string VariableName => m_objectName;

        /// <summary>绑定的全局变量类型。</summary>
        public Type VariableType => m_objectType;

        public override void allocateDefaultPins()
        {
            m_pin_value = createPin(m_objectName, PinDirection.Output, PinType.Data, m_objectType, null);
        }

        public override void excute(INodeManager manager)
        {
            m_pin_value.data = manager.GetDataObject(m_objectName);
        }
    }
}