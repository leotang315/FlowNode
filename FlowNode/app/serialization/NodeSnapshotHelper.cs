using System;
using System.Collections.Generic;
using System.Globalization;
using FlowNode.node;

namespace FlowNode.app.serialization
{
    /// <summary>
    /// 捕获/恢复单个节点的属性与引脚默认值，供文件序列化与复制粘贴共用。
    /// </summary>
    public static class NodeSnapshotHelper
    {
        private static readonly HashSet<string> SkippedPropertyNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Name", "Pins", "IsAutoRun", "NodePath"
        };

        public static List<PropertyData> CaptureProperties(NodeBase node)
        {
            var list = new List<PropertyData>();
            foreach (var property in node.GetType().GetProperties())
            {
                if (SkippedPropertyNames.Contains(property.Name))
                    continue;

                try
                {
                    var value = property.GetValue(node);
                    if (value == null)
                        continue;

                    list.Add(new PropertyData
                    {
                        Key = property.Name,
                        Value = Convert.ToString(value, CultureInfo.InvariantCulture),
                        TypeName = value.GetType().FullName
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error capturing property {property.Name}: {ex.Message}");
                }
            }
            return list;
        }

        public static List<PinData> CapturePins(NodeBase node)
        {
            var list = new List<PinData>();
            foreach (var pin in node.Pins)
            {
                list.Add(new PinData
                {
                    Name = pin.Name,
                    Direction = pin.direction,
                    PinType = pin.pinType,
                    DataType = pin.dataType?.FullName,
                    DefaultValue = pin.data != null
                        ? Convert.ToString(pin.data, CultureInfo.InvariantCulture)
                        : null,
                    ValueTypeName = pin.data?.GetType().FullName
                });
            }
            return list;
        }

        public static void ApplyProperties(NodeBase node, IEnumerable<PropertyData> properties)
        {
            if (properties == null)
                return;

            foreach (var property in properties)
            {
                if (property.Key == "NodePath")
                    continue;

                try
                {
                    var propertyInfo = node.GetType().GetProperty(property.Key);
                    if (propertyInfo == null || !propertyInfo.CanWrite)
                        continue;

                    if (property.TypeName != null)
                    {
                        var type = Type.GetType(property.TypeName);
                        if (type != null)
                        {
                            var convertedValue = Convert.ChangeType(property.Value, type, CultureInfo.InvariantCulture);
                            propertyInfo.SetValue(node, convertedValue);
                        }
                    }
                    else
                    {
                        propertyInfo.SetValue(node, property.Value);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error applying property {property.Key}: {ex.Message}");
                }
            }
        }

        public static void ApplyPinDefaults(NodeBase node, IEnumerable<PinData> pins)
        {
            if (pins == null)
                return;

            foreach (var pinData in pins)
            {
                if (pinData.DefaultValue == null)
                    continue;

                var pin = node.findPin(pinData.Name);
                if (pin == null)
                    continue;

                var value = PinValueConverter.ConvertStringToValue(
                    pinData.DefaultValue, pinData.ValueTypeName, pin.dataType);
                if (value != null)
                {
                    pin.data = value;
                }
            }
        }

        public static void Apply(NodeBase node, List<PropertyData> properties, List<PinData> pins)
        {
            ApplyProperties(node, properties);
            ApplyPinDefaults(node, pins);
        }
    }
}
