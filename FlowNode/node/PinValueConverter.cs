using System;
using System.Globalization;

namespace FlowNode.node
{
    /// <summary>
    /// 引脚字面值与字符串之间的转换工具，用于序列化往返。
    /// 仅支持原始可转换类型（数值/bool）、字符串与枚举；复杂对象不在范围内。
    /// </summary>
    public static class PinValueConverter
    {
        /// <summary>
        /// 将序列化/输入的字符串还原为目标类型的值。
        /// 优先使用 valueTypeName（保存时记录的实际值类型），其次回退到 fallbackType（引脚声明类型）。
        /// 无法还原时返回 null（调用方据此决定是否覆盖现有默认值）。
        /// </summary>
        public static object ConvertStringToValue(string text, string valueTypeName, Type fallbackType)
        {
            Type type = null;
            if (!string.IsNullOrEmpty(valueTypeName))
            {
                type = Type.GetType(valueTypeName);
            }
            if (type == null)
            {
                type = fallbackType;
            }
            // 引脚声明类型可能是 ref/out 形式（如 System.Int32&），取其元素类型
            if (type != null && type.IsByRef)
            {
                type = type.GetElementType();
            }
            if (type == null)
            {
                return text; // 无法判定类型时保留原始字符串
            }

            try
            {
                if (type == typeof(string)) return text;
                if (type.IsEnum) return Enum.Parse(type, text, true);
                if (typeof(IConvertible).IsAssignableFrom(type))
                {
                    return Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
                }
            }
            catch
            {
                // 转换失败则返回 null，由调用方保留原值
            }

            return null;
        }
    }
}
