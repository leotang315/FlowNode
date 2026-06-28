using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class MathExtendedTests
    {
        private static object Run(string method, params (string pin, object val)[] inputs)
        {
            var fn = new FunctionNode(null, typeof(MathOperator).GetMethod(method));
            fn.init();
            foreach (var input in inputs)
                fn.findPin(input.pin).data = input.val;
            fn.excute(new NodeManager());
            return fn.findPin("result").data;
        }

        [Test]
        public void MinMaxAbs_Int()
        {
            Assert.AreEqual(2, Run("min", ("a", 2), ("b", 5)));
            Assert.AreEqual(5, Run("max", ("a", 2), ("b", 5)));
            Assert.AreEqual(7, Run("abs", ("value", -7)));
        }

        [Test]
        public void FloatAdd_Sub_Mul_Div()
        {
            Assert.AreEqual(5.5f, Run("floatAdd", ("a", 2f), ("b", 3.5f)));
            Assert.AreEqual(-1.5f, Run("floatSub", ("a", 2f), ("b", 3.5f)));
            Assert.AreEqual(6f, Run("floatMul", ("a", 2f), ("b", 3f)));
            Assert.AreEqual(2f, Run("floatDiv", ("a", 6f), ("b", 3f)));
        }

        [Test]
        public void FloatDiv_ByZero_ReturnsZero()
        {
            Assert.AreEqual(0f, Run("floatDiv", ("a", 1f), ("b", 0f)));
        }

        [Test]
        public void FloatMinMaxAbs()
        {
            Assert.AreEqual(1.5f, Run("floatMin", ("a", 1.5f), ("b", 3f)));
            Assert.AreEqual(3f, Run("floatMax", ("a", 1.5f), ("b", 3f)));
            Assert.AreEqual(2f, Run("floatAbs", ("value", -2f)));
        }

        [Test]
        public void Clamp_And_Lerp_Int()
        {
            Assert.AreEqual(5, Run("clamp", ("value", 10), ("min", 0), ("max", 5)));
            Assert.AreEqual(0, Run("clamp", ("value", -3), ("min", 0), ("max", 5)));
            Assert.AreEqual(15, Run("lerp", ("a", 10), ("b", 20), ("t", 0.5f)));
        }

        [Test]
        public void FloatClamp_Lerp_Pow_And_Rounding()
        {
            Assert.AreEqual(2.5f, Run("floatClamp", ("value", 5f), ("min", 0f), ("max", 2.5f)));
            Assert.AreEqual(7.5f, Run("floatLerp", ("a", 5f), ("b", 10f), ("t", 0.5f)));
            Assert.AreEqual(8f, Run("floatPow", ("value", 2f), ("exponent", 3f)));
            Assert.AreEqual(2, Run("floor", ("value", 2.9f)));
            Assert.AreEqual(3, Run("ceil", ("value", 2.1f)));
            Assert.AreEqual(3, Run("round", ("value", 2.5f)));
        }
    }
}
