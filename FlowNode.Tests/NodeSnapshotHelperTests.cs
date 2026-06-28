using System.Linq;
using FlowNode.app.serialization;
using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class NodeSnapshotHelperTests
    {
        [Test]
        public void CaptureApply_RoundTrips_FunctionNodePinDefaults()
        {
            var addPath = NodeFactory.GetFunctionNodePaths().First(p => p.EndsWith("add"));
            var original = NodeFactory.CreateNode(addPath);
            original.findPin("a").data = 8;
            original.findPin("b").data = 5;

            var props = NodeSnapshotHelper.CaptureProperties(original);
            var pins = NodeSnapshotHelper.CapturePins(original);

            var restored = NodeFactory.CreateNode(addPath);
            NodeSnapshotHelper.Apply(restored, props, pins);

            Assert.AreEqual(8, restored.findPin("a").data);
            Assert.AreEqual(5, restored.findPin("b").data);

            restored.excute(new NodeManager());
            Assert.AreEqual(13, restored.findPin("result").data);
        }

        [Test]
        public void CaptureApply_RoundTrips_ConstantNodeValue()
        {
            var original = new IntConstantNode();
            original.init();
            original.Value = 77;

            var props = NodeSnapshotHelper.CaptureProperties(original);
            var pins = NodeSnapshotHelper.CapturePins(original);

            var restored = new IntConstantNode();
            restored.init();
            NodeSnapshotHelper.Apply(restored, props, pins);

            Assert.AreEqual(77, restored.Value);
            Assert.AreEqual(77, restored.findPin("Value").data);
        }

        [Test]
        public void CaptureApply_RoundTrips_LoopCountProperty()
        {
            var loopPath = NodeFactory.GetSystemNodePaths().First(p => p.EndsWith("Loop"));
            var original = NodeFactory.CreateNode(loopPath);
            original.GetType().GetProperty("LoopCount").SetValue(original, 5);

            var props = NodeSnapshotHelper.CaptureProperties(original);
            var pins = NodeSnapshotHelper.CapturePins(original);

            var restored = NodeFactory.CreateNode(loopPath);
            NodeSnapshotHelper.Apply(restored, props, pins);

            Assert.AreEqual(5, restored.GetType().GetProperty("LoopCount").GetValue(restored));
        }
    }
}
