using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class StringOperatorTests
    {
        private static object Run(string method, params (string pin, object val)[] inputs)
        {
            var fn = new FunctionNode(null, typeof(StringOperator).GetMethod(method));
            fn.init();
            foreach (var input in inputs)
                fn.findPin(input.pin).data = input.val;
            fn.excute(new NodeManager());
            return fn.findPin("result").data;
        }

        [Test]
        public void Concat_JoinsStrings()
        {
            Assert.AreEqual("HelloWorld", Run("Concat", ("a", "Hello"), ("b", "World")));
        }

        [Test]
        public void Concat_NullTreatedAsEmpty()
        {
            Assert.AreEqual("Hi", Run("Concat", ("a", "Hi"), ("b", null)));
        }

        [Test]
        public void Length_ReturnsCount()
        {
            Assert.AreEqual(5, Run("Length", ("value", "hello")));
            Assert.AreEqual(0, Run("Length", ("value", null)));
        }

        [Test]
        public void Contains_FindsSubstring()
        {
            Assert.AreEqual(true, Run("Contains", ("text", "hello world"), ("sub", "world")));
            Assert.AreEqual(false, Run("Contains", ("text", "hello"), ("sub", "x")));
        }

        [Test]
        public void Substring_ExtractsRange()
        {
            Assert.AreEqual("lo", Run("Substring", ("value", "hello"), ("start", 3), ("length", 2)));
            Assert.AreEqual(string.Empty, Run("Substring", ("value", "hello"), ("start", 10), ("length", 2)));
        }

        [Test]
        public void Trim_RemovesWhitespace()
        {
            Assert.AreEqual("hi", Run("Trim", ("value", "  hi  ")));
        }

        [Test]
        public void Replace_SubstitutesText()
        {
            Assert.AreEqual("bar bar", Run("Replace", ("value", "foo foo"), ("oldValue", "foo"), ("newValue", "bar")));
        }

        [Test]
        public void StartsWith_And_EndsWith()
        {
            Assert.AreEqual(true, Run("StartsWith", ("value", "hello"), ("prefix", "he")));
            Assert.AreEqual(true, Run("EndsWith", ("value", "hello"), ("suffix", "lo")));
            Assert.AreEqual(false, Run("StartsWith", ("value", "hello"), ("prefix", "lo")));
        }
    }
}
