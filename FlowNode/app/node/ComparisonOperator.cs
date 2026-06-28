using FlowNode.node.Attribute;

namespace FlowNode.node
{
    /// <summary>
    /// 比较运算：输入两个 int，输出 bool，常用于驱动 Branch.Condition。
    /// 均为自动运行的纯数据节点。
    /// </summary>
    [Node("Compare")]
    public class ComparisonOperator
    {
        [Function("greater", true)]
        public static void Greater(int a, int b, out bool result)
        {
            result = a > b;
        }

        [Function("greaterOrEqual", true)]
        public static void GreaterOrEqual(int a, int b, out bool result)
        {
            result = a >= b;
        }

        [Function("less", true)]
        public static void Less(int a, int b, out bool result)
        {
            result = a < b;
        }

        [Function("lessOrEqual", true)]
        public static void LessOrEqual(int a, int b, out bool result)
        {
            result = a <= b;
        }

        [Function("equal", true)]
        public static void Equal(int a, int b, out bool result)
        {
            result = a == b;
        }

        [Function("notEqual", true)]
        public static void NotEqual(int a, int b, out bool result)
        {
            result = a != b;
        }

        // --- float 比较 ---

        [Function("floatGreater", true)]
        public static void FloatGreater(float a, float b, out bool result)
        {
            result = a > b;
        }

        [Function("floatGreaterOrEqual", true)]
        public static void FloatGreaterOrEqual(float a, float b, out bool result)
        {
            result = a >= b;
        }

        [Function("floatLess", true)]
        public static void FloatLess(float a, float b, out bool result)
        {
            result = a < b;
        }

        [Function("floatLessOrEqual", true)]
        public static void FloatLessOrEqual(float a, float b, out bool result)
        {
            result = a <= b;
        }

        [Function("floatEqual", true)]
        public static void FloatEqual(float a, float b, out bool result)
        {
            result = a == b;
        }

        [Function("floatNotEqual", true)]
        public static void FloatNotEqual(float a, float b, out bool result)
        {
            result = a != b;
        }

        // --- string 比较 ---

        [Function("stringEqual", true)]
        public static void StringEqual(string a, string b, out bool result)
        {
            result = string.Equals(a, b);
        }

        [Function("stringNotEqual", true)]
        public static void StringNotEqual(string a, string b, out bool result)
        {
            result = !string.Equals(a, b);
        }
    }

    /// <summary>
    /// 布尔逻辑运算：用于组合多个条件。均为自动运行的纯数据节点。
    /// </summary>
    [Node("Logic")]
    public class LogicOperator
    {
        [Function("and", true)]
        public static void And(bool a, bool b, out bool result)
        {
            result = a && b;
        }

        [Function("or", true)]
        public static void Or(bool a, bool b, out bool result)
        {
            result = a || b;
        }

        [Function("not", true)]
        public static void Not(bool a, out bool result)
        {
            result = !a;
        }
    }
}
