using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace FlowNode.node
{
    /// <summary>
    /// 属性面板适配：在节点公有属性之外，额外暴露可编辑的数据输入引脚值（如 add 的 a/b）。
    /// </summary>
    public sealed class NodePropertySheet : ICustomTypeDescriptor
    {
        private static readonly HashSet<string> HiddenPropertyNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Pins", "NodePath", "method", "self", "VariableName", "VariableType"
        };

        public NodeBase Node { get; }
        public NodeManager NodeManager { get; }

        public NodePropertySheet(NodeBase node, NodeManager nodeManager = null)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            NodeManager = nodeManager;
        }

        public AttributeCollection GetAttributes() => TypeDescriptor.GetAttributes(Node, true);
        public string GetClassName() => TypeDescriptor.GetClassName(Node, true);
        public string GetComponentName() => Node.Name;
        public TypeConverter GetConverter() => TypeDescriptor.GetConverter(Node, true);
        public EventDescriptor GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(Node, true);
        public PropertyDescriptor GetDefaultProperty() => TypeDescriptor.GetDefaultProperty(Node, true);
        public object GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(Node, editorBaseType, true);
        public EventDescriptorCollection GetEvents() => TypeDescriptor.GetEvents(Node, true);
        public EventDescriptorCollection GetEvents(System.Attribute[] attributes) => TypeDescriptor.GetEvents(Node, attributes, true);
        public object GetPropertyOwner(PropertyDescriptor pd) => Node;

        public PropertyDescriptorCollection GetProperties() => GetProperties(null);

        public PropertyDescriptorCollection GetProperties(System.Attribute[] attributes)
        {
            var list = new List<PropertyDescriptor>();

            foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(Node, attributes, true))
            {
                if (HiddenPropertyNames.Contains(pd.Name))
                    continue;
                list.Add(pd);
            }

            var existingNames = new HashSet<string>(list.Select(p => p.Name), StringComparer.Ordinal);

            if (Node is GetObjectNode getNode && NodeManager != null)
            {
                list.Add(new GlobalVariablePropertyDescriptor(getNode, NodeManager));
                existingNames.Add("Value");
            }

            foreach (var pin in Node.Pins)
            {
                if (pin.direction != PinDirection.Input || pin.pinType != PinType.Data)
                    continue;
                if (existingNames.Contains(pin.Name))
                    continue;
                list.Add(new PinDataPropertyDescriptor(pin));
            }

            return new PropertyDescriptorCollection(list.ToArray());
        }
    }

    internal sealed class PinDataPropertyDescriptor : PropertyDescriptor
    {
        private readonly Pin pin;

        public PinDataPropertyDescriptor(Pin pin)
            : base(pin.Name, new System.Attribute[]
            {
                new CategoryAttribute("Pin Inputs"),
                new DisplayNameAttribute(pin.Name)
            })
        {
            this.pin = pin;
        }

        public override Type ComponentType => typeof(NodeBase);
        public override bool IsReadOnly => false;

        public override Type PropertyType
        {
            get
            {
                var t = pin.dataType;
                if (t != null && t.IsByRef)
                    t = t.GetElementType();
                return t ?? typeof(object);
            }
        }

        public override bool CanResetValue(object component) => false;

        public override object GetValue(object component)
        {
            if (pin.data != null)
                return pin.data;

            var t = PropertyType;
            if (t == typeof(string))
                return string.Empty;
            if (t.IsValueType)
                return Activator.CreateInstance(t);
            return null;
        }

        public override void ResetValue(object component) { }

        public override void SetValue(object component, object value)
        {
            if (value == null || PropertyType.IsInstanceOfType(value))
            {
                pin.data = value;
                return;
            }

            try
            {
                pin.data = Convert.ChangeType(value, PropertyType, CultureInfo.InvariantCulture);
            }
            catch
            {
                pin.data = value;
            }
        }

        public override bool ShouldSerializeValue(object component) => pin.data != null;
    }

    internal sealed class GlobalVariablePropertyDescriptor : PropertyDescriptor
    {
        private readonly GetObjectNode node;
        private readonly NodeManager manager;

        public GlobalVariablePropertyDescriptor(GetObjectNode node, NodeManager manager)
            : base("Value", new System.Attribute[]
            {
                new CategoryAttribute("Global Variable"),
                new DisplayNameAttribute(node.VariableName),
                new DescriptionAttribute("全局变量取值，Get 节点执行时从此读取。")
            })
        {
            this.node = node;
            this.manager = manager;
        }

        public string VariableName => node.VariableName;
        public Type VariableType => node.VariableType;

        public override Type ComponentType => typeof(NodeBase);
        public override bool IsReadOnly => false;

        public override Type PropertyType => node.VariableType ?? typeof(object);

        public override bool CanResetValue(object component) => false;

        public override object GetValue(object component)
        {
            if (manager.GetDataObjectType(node.VariableName) == null)
            {
                if (PropertyType.IsValueType)
                    return Activator.CreateInstance(PropertyType);
                return null;
            }

            return manager.GetDataObject(node.VariableName);
        }

        public override void ResetValue(object component) { }

        public override void SetValue(object component, object value)
        {
            if (value == null || PropertyType.IsInstanceOfType(value))
            {
                manager.SetDataObject(node.VariableName, value, PropertyType);
            }
            else
            {
                manager.SetDataObject(
                    node.VariableName,
                    Convert.ChangeType(value, PropertyType, CultureInfo.InvariantCulture),
                    PropertyType);
            }

            node.RefreshOutputFrom(manager);
        }

        public override bool ShouldSerializeValue(object component) => true;
    }
}
