using System;
using System.Globalization;
using FlowNode.node;

namespace FlowNode.app.command
{
    /// <summary>修改节点公有属性（PropertyGrid / 属性面板），支持 Undo。</summary>
    public class SetNodePropertyCommand : ICommand
    {
        private readonly NodeBase node;
        private readonly string propertyName;
        private readonly object oldValue;
        private readonly object newValue;

        public SetNodePropertyCommand(NodeBase node, string propertyName, object oldValue, object newValue)
        {
            this.node = node;
            this.propertyName = propertyName;
            this.oldValue = oldValue;
            this.newValue = newValue;
        }

        public void Execute() => Apply(newValue);

        public void Undo() => Apply(oldValue);

        private void Apply(object value)
        {
            var prop = node.GetType().GetProperty(propertyName);
            if (prop == null || !prop.CanWrite)
                return;

            if (value == null || prop.PropertyType.IsInstanceOfType(value))
            {
                prop.SetValue(node, value);
                return;
            }

            try
            {
                prop.SetValue(node, Convert.ChangeType(value, prop.PropertyType, CultureInfo.InvariantCulture));
            }
            catch
            {
                prop.SetValue(node, value);
            }
        }
    }

    /// <summary>修改数据输入引脚默认值（Pin Inputs），支持 Undo。</summary>
    public class SetPinDataCommand : ICommand
    {
        private readonly NodeBase node;
        private readonly string pinName;
        private readonly object oldValue;
        private readonly object newValue;

        public SetPinDataCommand(NodeBase node, string pinName, object oldValue, object newValue)
        {
            this.node = node;
            this.pinName = pinName;
            this.oldValue = oldValue;
            this.newValue = newValue;
        }

        public void Execute() => Apply(newValue);

        public void Undo() => Apply(oldValue);

        private void Apply(object value)
        {
            var pin = node.findPin(pinName);
            if (pin == null)
                return;

            pin.data = value;
        }
    }
}
