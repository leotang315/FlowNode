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

        [Function("min", true)]
        public static void min(int a, int b, out int result)
        {
            result = a < b ? a : b;
        }

        [Function("max", true)]
        public static void max(int a, int b, out int result)
        {
            result = a > b ? a : b;
        }

        [Function("abs", true)]
        public static void abs(int value, out int result)
        {
            result = Math.Abs(value);
        }

        [Function("floatAdd", true)]
        public static void floatAdd(float a, float b, out float result)
        {
            result = a + b;
        }

        [Function("floatSub", true)]
        public static void floatSub(float a, float b, out float result)
        {
            result = a - b;
        }

        [Function("floatMul", true)]
        public static void floatMul(float a, float b, out float result)
        {
            result = a * b;
        }

        [Function("floatDiv", true)]
        public static void floatDiv(float a, float b, out float result)
        {
            result = b != 0f ? a / b : 0f;
        }

        [Function("floatMin", true)]
        public static void floatMin(float a, float b, out float result)
        {
            result = a < b ? a : b;
        }

        [Function("floatMax", true)]
        public static void floatMax(float a, float b, out float result)
        {
            result = a > b ? a : b;
        }

        [Function("floatAbs", true)]
        public static void floatAbs(float value, out float result)
        {
            result = Math.Abs(value);
        }
    }
}
