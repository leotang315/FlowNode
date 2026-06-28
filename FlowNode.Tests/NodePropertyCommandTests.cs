using System.Linq;
using FlowNode.app.command;
using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class NodePropertyCommandTests
    {
        [Test]
        public void SetNodeProperty_UndoRedo_RestoresLoopCount()
        {
            var loop = new LoopNode();
            loop.init();
            loop.LoopCount = 1;

            var cmd = new SetNodePropertyCommand(loop, "LoopCount", 1, 5);
            cmd.Execute();
            Assert.AreEqual(5, loop.LoopCount);

            cmd.Undo();
            Assert.AreEqual(1, loop.LoopCount);

            cmd.Execute();
            Assert.AreEqual(5, loop.LoopCount);
        }

        [Test]
        public void SetNodeProperty_UndoRedo_SyncsConstantValuePin()
        {
            var c = new IntConstantNode();
            c.init();
            c.Value = 3;

            var cmd = new SetNodePropertyCommand(c, "Value", 3, 9);
            cmd.Execute();
            Assert.AreEqual(9, c.Value);
            Assert.AreEqual(9, c.findPin("Value").data);

            cmd.Undo();
            Assert.AreEqual(3, c.Value);
            Assert.AreEqual(3, c.findPin("Value").data);
        }

        [Test]
        public void SetPinData_UndoRedo_RestoresFunctionInput()
        {
            var addPath = NodeFactory.GetFunctionNodePaths().First(p => p.EndsWith("add"));
            var node = NodeFactory.CreateNode(addPath);

            var cmd = new SetPinDataCommand(node, "a", 0, 12);
            cmd.Execute();
            Assert.AreEqual(12, node.findPin("a").data);

            cmd.Undo();
            Assert.AreEqual(0, node.findPin("a").data);
        }
    }
}
