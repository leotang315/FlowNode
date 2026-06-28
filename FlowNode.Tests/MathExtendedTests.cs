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
    }
}
