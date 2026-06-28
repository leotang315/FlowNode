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
    }
}
