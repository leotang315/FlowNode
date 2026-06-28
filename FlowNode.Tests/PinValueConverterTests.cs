using System;
using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class PinValueConverterTests
    {
        [Test]
        public void ConvertStringToValue_ParsesByValueTypeName()
        {
            Assert.AreEqual(7, PinValueConverter.ConvertStringToValue("7", typeof(int).FullName, null));
            Assert.AreEqual(true, PinValueConverter.ConvertStringToValue("true", typeof(bool).FullName, null));
            Assert.AreEqual(1.5f, PinValueConverter.ConvertStringToValue("1.5", typeof(float).FullName, null));
        }

        [Test]
        public void ConvertStringToValue_FallsBackToDeclaredType()
        {
            Assert.AreEqual(42, PinValueConverter.ConvertStringToValue("42", null, typeof(int)));
        }

        [Test]
        public void ConvertStringToValue_ReturnsNull_OnUnparseableNumber()
        {
            Assert.IsNull(PinValueConverter.ConvertStringToValue("abc", typeof(int).FullName, typeof(int)));
        }

        [Test]
        public void ConvertStringToValue_StringPassesThrough()
        {
            Assert.AreEqual("hello", PinValueConverter.ConvertStringToValue("hello", typeof(string).FullName, typeof(string)));
        }
    }
}
