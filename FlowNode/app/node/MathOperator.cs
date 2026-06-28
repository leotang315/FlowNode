using FlowNode.node.Attribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowNode.node
{
    [Node]
    public class MathOperator
    {
        [Function("", true)]
        public static void add(int a, int b, out int result)
        {
            result = a + b;
        }

        [Function("", true)]
        public static void sub(int a, int b, out int result)
        {
            result = a - b;
        }

        [Function("mul", true)]
        public static void mul(int a, int b, out int result)
        {
            result = a * b;
        }

        [Function("div", true)]
        public static void div(int a, int b, out int result)
        {
            // 防止除零导致执行中断，除数为 0 时返回 0
            result = b != 0 ? a / b : 0;
        }

        [Function("mod", true)]
        public static void mod(int a, int b, out int result)
        {
            result = b != 0 ? a % b : 0;
        }
    }
}
