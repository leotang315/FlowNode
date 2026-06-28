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

        [Function("stringToInt", true)]
        public static void StringToInt(string value, out int result)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
            {
                result = 0;
            }
        }

        [Function("stringToFloat", true)]
        public static void StringToFloat(string value, out float result)
        {
            if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            {
                result = 0f;
            }
        }

        [Function("stringToBool", true)]
        public static void StringToBool(string value, out bool result)
        {
            if (!bool.TryParse(value, out result))
            {
                result = false;
            }
        }
    }
}
