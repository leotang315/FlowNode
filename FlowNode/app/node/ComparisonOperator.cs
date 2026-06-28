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
