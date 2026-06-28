using System;
using System.Linq;
using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class NodeManagerTests
    {
        private static TestNode NewTestNode()
        {
            var node = new TestNode();
            node.init();
            return node;
        }

        private static FunctionNode NewAddNode()
        {
            var method = typeof(MathOperator).GetMethod("add");
            var node = new FunctionNode(null, method);
            node.init();
            return node;
        }

        [Test]
        public void AddConnector_ValidExecuteLink_IsAccepted()
        {
            var mgr = new NodeManager();
            var a = NewTestNode();
            var b = NewTestNode();
            mgr.addNode(a);
            mgr.addNode(b);

            mgr.addConnector(a.findPin("Out"), b.findPin("In"));

            Assert.AreEqual(1, mgr.getConnectors().Count);
        }

        [Test]
        public void AddConnector_InputToOutput_Throws()
        {
            var mgr = new NodeManager();
            var a = NewTestNode();
            var b = NewTestNode();
            mgr.addNode(a);
            mgr.addNode(b);

            // src 必须是 Output，dst 必须是 Input；此处方向反了
            Assert.Throws<InvalidOperationException>(
                () => mgr.addConnector(a.findPin("In"), b.findPin("Out")));
        }

        [Test]
        public void AddConnector_ExecuteToData_Throws()
        {
            var mgr = new NodeManager();
            var exec = NewTestNode();
            var fn = NewAddNode();
            mgr.addNode(exec);
            mgr.addNode(fn);

            // 执行引脚不能连到数据引脚
            Assert.Throws<InvalidOperationException>(
                () => mgr.addConnector(exec.findPin("Out"), fn.findPin("a")));
        }

        [Test]
        public void Validate_EmptyGraph_ReturnsError()
        {
            var mgr = new NodeManager();
            var errors = mgr.Validate();
            Assert.IsNotEmpty(errors);
        }

        [Test]
        public void Validate_SingleEntryNode_IsValid()
        {
            var mgr = new NodeManager();
            mgr.addNode(NewTestNode());

            var errors = mgr.Validate();

            Assert.IsEmpty(errors);
        }

        [Test]
        public void Validate_ExecuteCycle_ReturnsError()
        {
            var mgr = new NodeManager();
            var a = NewTestNode();
            var b = NewTestNode();
            mgr.addNode(a);
            mgr.addNode(b);

            mgr.addConnector(a.findPin("Out"), b.findPin("In"));
            mgr.addConnector(b.findPin("Out"), a.findPin("In"));

            var errors = mgr.Validate();

            Assert.IsNotEmpty(errors);
        }

        [Test]
        public void ValidateWarnings_IsolatedNode_ReturnsWarning()
        {
            var mgr = new NodeManager();
            mgr.addNode(NewTestNode());

            var warnings = mgr.ValidateWarnings();

            Assert.IsNotEmpty(warnings);
        }

        [Test]
        public void DataObjects_SetAndGet_RoundTrips()
        {
            var mgr = new NodeManager();
            mgr.SetDataObject("count", 42, typeof(int));

            Assert.AreEqual(42, mgr.GetDataObject("count"));
            Assert.AreEqual(typeof(int), mgr.GetDataObjectType("count"));
            CollectionAssert.Contains(mgr.GetAllDataObjectKeys(), "count");

            mgr.RemoveDataObject("count");
            Assert.IsNull(mgr.GetDataObject("count"));
        }
    }
}
