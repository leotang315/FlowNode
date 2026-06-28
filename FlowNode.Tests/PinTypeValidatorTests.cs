using System;
using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class PinTypeValidatorTests
    {
        [Test]
        public void SameType_IsCompatible()
        {
            Assert.IsTrue(PinTypeValidator.AreTypesCompatible(typeof(int), typeof(int)));
        }

        [Test]
        public void DerivedToBase_IsCompatible()
        {
            // int 可赋值给 object
            Assert.IsTrue(PinTypeValidator.AreTypesCompatible(typeof(int), typeof(object)));
        }

        [Test]
        public void UnrelatedTypes_AreIncompatible()
        {
            Assert.IsFalse(PinTypeValidator.AreTypesCompatible(typeof(string), typeof(int)));
        }

        [Test]
        public void NullType_IsIncompatible()
        {
            Assert.IsFalse(PinTypeValidator.AreTypesCompatible(null, typeof(int)));
            Assert.IsFalse(PinTypeValidator.AreTypesCompatible(typeof(int), null));
        }

        [Test]
        public void ByRefType_IsUnwrappedAndCompatible()
        {
            // out/ref 参数类型（如 int&）应能与 int 兼容
            Assert.IsTrue(PinTypeValidator.AreTypesCompatible(typeof(int).MakeByRefType(), typeof(int)));
        }

        [Test]
        public void CanConnect_RejectsIncompatibleDataTypes_WithMessage()
        {
            var hostA = new PrintNode();
            hostA.init();
            var hostB = new PrintNode();
            hostB.init();
            var src = new Pin(hostA) { direction = PinDirection.Output, pinType = PinType.Data, dataType = typeof(int) };
            var dst = new Pin(hostB) { direction = PinDirection.Input, pinType = PinType.Data, dataType = typeof(bool) };

            Assert.IsFalse(PinTypeValidator.CanConnect(src, dst, out string error));
            Assert.IsNotNull(error);
            StringAssert.Contains("Int32", error);
            StringAssert.Contains("Boolean", error);
        }

        [Test]
        public void CanConnect_AllowsExecutePins()
        {
            var hostA = new PrintNode();
            hostA.init();
            var hostB = new PrintNode();
            hostB.init();
            var src = new Pin(hostA) { direction = PinDirection.Output, pinType = PinType.Execute };
            var dst = new Pin(hostB) { direction = PinDirection.Input, pinType = PinType.Execute };

            Assert.IsTrue(PinTypeValidator.CanConnect(src, dst, out string error));
            Assert.IsNull(error);
        }
    }
}
