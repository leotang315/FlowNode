using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class ConvertTests
    {
        private static object RunFunc(string method, params (string pin, object val)[] inputs)
        {
            var fn = new FunctionNode(null, typeof(ConvertOperator).GetMethod(method));
            fn.init();
            foreach (var input in inputs)
                fn.findPin(input.pin).data = input.val;
            fn.excute(new NodeManager());
            return fn.findPin("result").data;
        }

        [Test]
        public void IntToFloat_Converts()
        {
            Assert.AreEqual(3f, RunFunc("IntToFloat", ("value", 3)));
        }

        [Test]
        public void FloatToInt_Truncates()
        {
            Assert.AreEqual(3, RunFunc("FloatToInt", ("value", 3.9f)));
        }

        [Test]
        public void IntToString_UsesInvariantCulture()
        {
            Assert.AreEqual("42", RunFunc("IntToString", ("value", 42)));
        }

        [Test]
        public void FloatToString_UsesInvariantCulture()
        {
            Assert.AreEqual("1.5", RunFunc("FloatToString", ("value", 1.5f)));
        }

        [Test]
        public void BoolToString_ReturnsTrueFalse()
        {
            Assert.AreEqual("True", RunFunc("BoolToString", ("value", true)));
            Assert.AreEqual("False", RunFunc("BoolToString", ("value", false)));
        }

        [Test]
        public void ConvertNodes_AreDiscoveredViaReflection()
        {
            var paths = NodeFactory.GetFunctionNodePaths();
            Assert.IsTrue(paths.Exists(p => p.EndsWith("intToFloat")));
            Assert.IsTrue(paths.Exists(p => p.EndsWith("floatToInt")));
        }

        [Test]
        public void StringToInt_ParsesOrZero()
        {
            Assert.AreEqual(42, RunFunc("StringToInt", ("value", "42")));
            Assert.AreEqual(0, RunFunc("StringToInt", ("value", "not-a-number")));
        }

        [Test]
        public void StringToFloat_ParsesOrZero()
        {
            Assert.AreEqual(1.5f, RunFunc("StringToFloat", ("value", "1.5")));
            Assert.AreEqual(0f, RunFunc("StringToFloat", ("value", "x")));
        }

        [Test]
        public void StringToBool_ParsesOrFalse()
        {
            Assert.AreEqual(true, RunFunc("StringToBool", ("value", "True")));
            Assert.AreEqual(false, RunFunc("StringToBool", ("value", "maybe")));
        }
    }
}
