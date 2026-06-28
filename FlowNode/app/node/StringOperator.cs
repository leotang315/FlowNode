using FlowNode.node.Attribute;

namespace FlowNode.node
{
    /// <summary>
    /// 字符串运算：自动运行的纯数据节点。
    /// </summary>
    [Node("String")]
    public class StringOperator
    {
        [Function("concat", true)]
        public static void Concat(string a, string b, out string result)
        {
            result = (a ?? string.Empty) + (b ?? string.Empty);
        }

        [Function("length", true)]
        public static void Length(string value, out int result)
        {
            result = value?.Length ?? 0;
        }

        [Function("contains", true)]
        public static void Contains(string text, string sub, out bool result)
        {
            if (text == null || sub == null)
            {
                result = false;
                return;
            }
            result = text.Contains(sub);
        }

        [Function("substring", true)]
        public static void Substring(string value, int start, int length, out string result)
        {
            if (string.IsNullOrEmpty(value) || start < 0 || length <= 0 || start >= value.Length)
            {
                result = string.Empty;
                return;
            }

            if (start + length > value.Length)
                length = value.Length - start;

            result = value.Substring(start, length);
        }

        [Function("trim", true)]
        public static void Trim(string value, out string result)
        {
            result = value?.Trim() ?? string.Empty;
        }

        [Function("replace", true)]
        public static void Replace(string value, string oldValue, string newValue, out string result)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(oldValue))
            {
                result = value ?? string.Empty;
                return;
            }

            result = value.Replace(oldValue, newValue ?? string.Empty);
        }

        [Function("startsWith", true)]
        public static void StartsWith(string value, string prefix, out bool result)
        {
            result = value != null && prefix != null && value.StartsWith(prefix);
        }

        [Function("endsWith", true)]
        public static void EndsWith(string value, string suffix, out bool result)
        {
            result = value != null && suffix != null && value.EndsWith(suffix);
        }
    }
}
