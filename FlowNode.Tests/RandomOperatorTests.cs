using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class RandomOperatorTests
    {
        private static object Run(string method, params (string pin, object val)[] inputs)
        {
            var fn = new FunctionNode(null, typeof(RandomOperator).GetMethod(method));
            fn.init();
            foreach (var input in inputs)
                fn.findPin(input.pin).data = input.val;
            fn.excute(new NodeManager());
            return fn.findPin("result").data;
        }

        [Test]
        public void RandomInt_StaysWithinRange()
        {
            for (int i = 0; i < 50; i++)
            {
                var value = (int)Run("RandomInt", ("min", 3), ("max", 8));
                Assert.GreaterOrEqual(value, 3);
                Assert.Less(value, 8);
            }
        }

        [Test]
        public void RandomFloat_StaysWithinRange()
        {
            for (int i = 0; i < 50; i++)
            {
                var value = (float)Run("RandomFloat", ("min", 1f), ("max", 2f));
                Assert.GreaterOrEqual(value, 1f);
                Assert.Less(value, 2f);
            }
        }

        [Test]
        public void RandomInt_InvalidRange_ReturnsMin()
        {
            Assert.AreEqual(5, Run("RandomInt", ("min", 5), ("max", 5)));
        }
    }
}
