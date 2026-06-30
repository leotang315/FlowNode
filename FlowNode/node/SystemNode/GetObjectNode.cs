using System;
using System.Globalization;
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

        /// <summary>从 NodeManager 全局变量刷新输出引脚，便于画布副标题与未执行前预览。</summary>
        public void RefreshOutputFrom(INodeManager manager)
        {
            if (manager == null || m_pin_value == null)
                return;

            if (manager.GetDataObjectType(m_objectName) == null)
            {
                m_pin_value.data = null;
                return;
            }

            m_pin_value.data = manager.GetDataObject(m_objectName);
        }

        public override string GetDisplaySubtitle()
        {
            if (m_pin_value?.data != null)
                return Convert.ToString(m_pin_value.data, CultureInfo.InvariantCulture);

            return "未设置";
        }

        public override void excute(INodeManager manager)
        {
            RefreshOutputFrom(manager);
        }
    }
}