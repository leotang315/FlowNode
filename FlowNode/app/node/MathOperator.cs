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
    }
}
