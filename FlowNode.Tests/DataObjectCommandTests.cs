using System;
using FlowNode.app.command;
using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class DataObjectCommandTests
    {
        [Test]
        public void AddDataObject_UndoRedo_RoundTrips()
        {
            var mgr = new NodeManager();
            var cm = new CommandManager();

            cm.ExecuteCommand(new AddDataObjectCommand(mgr, "score", 10, typeof(int)));
            Assert.AreEqual(10, mgr.GetDataObject("score"));

            cm.Undo();
            Assert.IsNull(mgr.GetDataObject("score"));

            cm.Redo();
            Assert.AreEqual(10, mgr.GetDataObject("score"));
        }

        [Test]
        public void AddDataObject_OverwritesExisting_UndoRestoresOldValue()
        {
            var mgr = new NodeManager();
            var cm = new CommandManager();
            mgr.SetDataObject("name", "alice", typeof(string));

            cm.ExecuteCommand(new AddDataObjectCommand(mgr, "name", "bob", typeof(string)));
            Assert.AreEqual("bob", mgr.GetDataObject("name"));

            cm.Undo();
            Assert.AreEqual("alice", mgr.GetDataObject("name"));
        }

        [Test]
        public void RemoveDataObject_UndoRedo_RoundTrips()
        {
            var mgr = new NodeManager();
            var cm = new CommandManager();
            mgr.SetDataObject("flag", true, typeof(bool));

            cm.ExecuteCommand(new RemoveDataObjectCommand(mgr, "flag"));
            Assert.IsNull(mgr.GetDataObject("flag"));

            cm.Undo();
            Assert.AreEqual(true, mgr.GetDataObject("flag"));

            cm.Redo();
            Assert.IsNull(mgr.GetDataObject("flag"));
        }

        [Test]
        public void UpdateDataObject_UndoRedo_RoundTrips()
        {
            var mgr = new NodeManager();
            var cm = new CommandManager();
            mgr.SetDataObject("ratio", 0.5, typeof(double));

            cm.ExecuteCommand(new UpdateDataObjectCommand(mgr, "ratio", 0.75, typeof(double)));
            Assert.AreEqual(0.75, Convert.ToDouble(mgr.GetDataObject("ratio")), 1e-9);

            cm.Undo();
            Assert.AreEqual(0.5, Convert.ToDouble(mgr.GetDataObject("ratio")), 1e-9);

            cm.Redo();
            Assert.AreEqual(0.75, Convert.ToDouble(mgr.GetDataObject("ratio")), 1e-9);
        }
    }
}
