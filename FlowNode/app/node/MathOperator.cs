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

        [Function("clamp", true)]
        public static void clamp(int value, int min, int max, out int result)
        {
            if (min > max)
            {
                var tmp = min;
                min = max;
                max = tmp;
            }
            result = Math.Max(min, Math.Min(max, value));
        }

        [Function("lerp", true)]
        public static void lerp(int a, int b, float t, out int result)
        {
            result = (int)(a + (b - a) * t);
        }

        [Function("floatClamp", true)]
        public static void floatClamp(float value, float min, float max, out float result)
        {
            if (min > max)
            {
                var tmp = min;
                min = max;
                max = tmp;
            }
            result = Math.Max(min, Math.Min(max, value));
        }

        [Function("floatLerp", true)]
        public static void floatLerp(float a, float b, float t, out float result)
        {
            result = a + (b - a) * t;
        }

        [Function("floatPow", true)]
        public static void floatPow(float value, float exponent, out float result)
        {
            result = (float)Math.Pow(value, exponent);
        }

        [Function("floor", true)]
        public static void floor(float value, out int result)
        {
            result = (int)Math.Floor(value);
        }

        [Function("ceil", true)]
        public static void ceil(float value, out int result)
        {
            result = (int)Math.Ceiling(value);
        }

        [Function("round", true)]
        public static void round(float value, out int result)
        {
            result = (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }
}
