using FlowNode1.node.Attribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowNode1.node
{
    [Node]
    public class MathOperator
    {
        [Function("", true)]
        public void add(int a, int b, int result)
        {
            result = a + b;
        }
    }
}
