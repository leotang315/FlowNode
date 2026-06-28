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
    }
}
