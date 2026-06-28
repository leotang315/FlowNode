using System.Collections.Generic;
using System.Drawing;
using FlowNode.app.command;
using FlowNode.app.view;
using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class CommandManagerTests
    {
        [Test]
        public void AddNodeData_UndoRedo_RoundTrips()
        {
            var mgr = new NodeManager();
            var cmdMgr = new CommandManager();
            var node = new PrintNode();
            node.init();

            cmdMgr.ExecuteCommand(new AddNodeDataCommand(mgr, node));
            Assert.AreEqual(1, mgr.getNodes().Count);

            cmdMgr.Undo();
            Assert.AreEqual(0, mgr.getNodes().Count);

            cmdMgr.Redo();
            Assert.AreEqual(1, mgr.getNodes().Count);
        }

        [Test]
        public void AddConnector_UndoRedo_RoundTrips()
        {
            var mgr = new NodeManager();
            var cmdMgr = new CommandManager();
            var src = new IntConstantNode();
            src.init();
            var dst = new PrintNode();
            dst.init();
            mgr.addNode(src);
            mgr.addNode(dst);

            cmdMgr.ExecuteCommand(new AddConnectorDataCommand(mgr, src.findPin("Value"), dst.findPin("Value")));
            Assert.AreEqual(1, mgr.getConnectors().Count);

            cmdMgr.Undo();
            Assert.AreEqual(0, mgr.getConnectors().Count);

            cmdMgr.Redo();
            Assert.AreEqual(1, mgr.getConnectors().Count);
        }

        [Test]
        public void RemoveNode_UndoRestoresNodeAndConnectors()
        {
            var mgr = new NodeManager();
            var cmdMgr = new CommandManager();
            var a = new IntConstantNode();
            a.init();
            var b = new PrintNode();
            b.init();
            mgr.addNode(a);
            mgr.addNode(b);
            mgr.addConnector(a.findPin("Value"), b.findPin("Value"));

            cmdMgr.ExecuteCommand(new RemoveNodeDataCommand(mgr, a));
            Assert.AreEqual(0, mgr.getConnectors().Count);
            Assert.AreEqual(1, mgr.getNodes().Count);

            cmdMgr.Undo();
            Assert.AreEqual(2, mgr.getNodes().Count);
            Assert.AreEqual(1, mgr.getConnectors().Count);
        }

        [Test]
        public void RemoveConnector_UndoRedo_RoundTrips()
        {
            var mgr = new NodeManager();
            var cmdMgr = new CommandManager();
            var src = new IntConstantNode();
            src.init();
            var dst = new PrintNode();
            dst.init();
            mgr.addNode(src);
            mgr.addNode(dst);
            mgr.addConnector(src.findPin("Value"), dst.findPin("Value"));
            var connector = mgr.getConnectors()[0];

            cmdMgr.ExecuteCommand(new RemoveConnectorDataCommand(mgr, connector));
            Assert.AreEqual(0, mgr.getConnectors().Count);

            cmdMgr.Undo();
            Assert.AreEqual(1, mgr.getConnectors().Count);

            cmdMgr.Redo();
            Assert.AreEqual(0, mgr.getConnectors().Count);
        }

        [Test]
        public void CommandGroup_UndoRedo_AsSingleOperation()
        {
            var mgr = new NodeManager();
            var cmdMgr = new CommandManager();
            var a = new PrintNode();
            a.init();
            var b = new PrintNode();
            b.init();

            using (cmdMgr.BeginCommandGroup())
            {
                cmdMgr.ExecuteCommand(new AddNodeDataCommand(mgr, a));
                cmdMgr.ExecuteCommand(new AddNodeDataCommand(mgr, b));
            }

            Assert.AreEqual(2, mgr.getNodes().Count);

            cmdMgr.Undo();
            Assert.AreEqual(0, mgr.getNodes().Count);

            cmdMgr.Redo();
            Assert.AreEqual(2, mgr.getNodes().Count);
        }

        [Test]
        public void NewCommand_ClearsRedoStack()
        {
            var mgr = new NodeManager();
            var cmdMgr = new CommandManager();
            var a = new PrintNode();
            a.init();
            var b = new PrintNode();
            b.init();

            cmdMgr.ExecuteCommand(new AddNodeDataCommand(mgr, a));
            cmdMgr.Undo();
            Assert.IsTrue(cmdMgr.CanRedo);

            cmdMgr.ExecuteCommand(new AddNodeDataCommand(mgr, b));
            Assert.IsFalse(cmdMgr.CanRedo);
        }

        [Test]
        public void MoveNodeView_UndoRedo_RoundTrips()
        {
            var views = new Dictionary<INode, NodeView>();
            var cmdMgr = new CommandManager();
            var node = new PrintNode();
            node.init();
            var view = new DefaultNodeView(node, new Point(0, 0));
            views[node] = view;

            cmdMgr.ExecuteCommand(new MoveNodeViewCommand(view, new Point(0, 0), new Point(50, 60)));
            Assert.AreEqual(new Point(50, 60), view.Bounds.Location);

            cmdMgr.Undo();
            Assert.AreEqual(new Point(0, 0), view.Bounds.Location);

            cmdMgr.Redo();
            Assert.AreEqual(new Point(50, 60), view.Bounds.Location);
        }
    }
}
