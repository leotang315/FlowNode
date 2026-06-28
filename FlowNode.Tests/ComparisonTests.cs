using System;
using FlowNode;
using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class ComparisonTests
    {
        private static object RunFunc(Type owner, string method, params (string pin, object val)[] inputs)
        {
            var fn = new FunctionNode(null, owner.GetMethod(method));
            fn.init();
            foreach (var input in inputs)
                fn.findPin(input.pin).data = input.val;
            fn.excute(new NodeManager());
            return fn.findPin("result").data;
        }

        [Test]
        public void Greater_True()
        {
            Assert.AreEqual(true, RunFunc(typeof(ComparisonOperator), "Greater", ("a", 5), ("b", 3)));
        }

        [Test]
        public void Greater_False()
        {
            Assert.AreEqual(false, RunFunc(typeof(ComparisonOperator), "Greater", ("a", 2), ("b", 3)));
        }

        [Test]
        public void Equal_And_NotEqual()
        {
            Assert.AreEqual(true, RunFunc(typeof(ComparisonOperator), "Equal", ("a", 4), ("b", 4)));
            Assert.AreEqual(true, RunFunc(typeof(ComparisonOperator), "NotEqual", ("a", 4), ("b", 5)));
        }

        [Test]
        public void Logic_And_Or_Not()
        {
            Assert.AreEqual(false, RunFunc(typeof(LogicOperator), "And", ("a", true), ("b", false)));
            Assert.AreEqual(true, RunFunc(typeof(LogicOperator), "Or", ("a", true), ("b", false)));
            Assert.AreEqual(false, RunFunc(typeof(LogicOperator), "Not", ("a", true)));
        }

        [Test]
        public void Math_Mul_Div_Mod()
        {
            Assert.AreEqual(12, RunFunc(typeof(MathOperator), "mul", ("a", 3), ("b", 4)));
            Assert.AreEqual(5, RunFunc(typeof(MathOperator), "div", ("a", 10), ("b", 2)));
            Assert.AreEqual(1, RunFunc(typeof(MathOperator), "mod", ("a", 7), ("b", 3)));
        }

        [Test]
        public void Div_ByZero_ReturnsZero_NoThrow()
        {
            Assert.AreEqual(0, RunFunc(typeof(MathOperator), "div", ("a", 1), ("b", 0)));
        }

        [Test]
        public void ComparisonResult_CanDriveBranchCondition()
        {
            var mgr = new NodeManager();
            var cmp = new FunctionNode(null, typeof(ComparisonOperator).GetMethod("Greater"));
            cmp.init();
            var branch = new BranchNode();
            branch.init();
            mgr.addNode(cmp);
            mgr.addNode(branch);

            // 比较节点的 bool 输出可连到 Branch 的 Condition
            mgr.addConnector(cmp.findPin("result"), branch.findPin("Condition"));

            Assert.AreEqual(1, mgr.getConnectors().Count);
        }

        [Test]
        public void FloatEqual_Works()
        {
            Assert.AreEqual(true, RunFunc(typeof(ComparisonOperator), "FloatEqual", ("a", 1.5f), ("b", 1.5f)));
            Assert.AreEqual(false, RunFunc(typeof(ComparisonOperator), "FloatGreater", ("a", 1.0f), ("b", 2.0f)));
        }

        [Test]
        public void StringEqual_IsCaseSensitive()
        {
            Assert.AreEqual(true, RunFunc(typeof(ComparisonOperator), "StringEqual", ("a", "hello"), ("b", "hello")));
            Assert.AreEqual(false, RunFunc(typeof(ComparisonOperator), "StringEqual", ("a", "Hello"), ("b", "hello")));
            Assert.AreEqual(true, RunFunc(typeof(ComparisonOperator), "StringNotEqual", ("a", "a"), ("b", "b")));
        }
    }
}
