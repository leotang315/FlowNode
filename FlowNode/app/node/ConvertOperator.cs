using System.Globalization;
using FlowNode.node.Attribute;

namespace FlowNode.node
{
    /// <summary>
    /// 类型转换：自动运行的纯数据节点，输出转换后的值。
    /// </summary>
    [Node("Convert")]
    public class ConvertOperator
    {
        [Function("intToFloat", true)]
        public static void IntToFloat(int value, out float result)
        {
            result = value;
        }

        [Function("floatToInt", true)]
        public static void FloatToInt(float value, out int result)
        {
            result = (int)value;
        }

        [Function("intToString", true)]
        public static void IntToString(int value, out string result)
        {
            result = value.ToString(CultureInfo.InvariantCulture);
        }

        [Function("floatToString", true)]
        public static void FloatToString(float value, out string result)
        {
            result = value.ToString(CultureInfo.InvariantCulture);
        }

        [Function("boolToString", true)]
        public static void BoolToString(bool value, out string result)
        {
            result = value ? "True" : "False";
        }
    }
}
