using System.Linq;
using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class JsonOperatorTests
    {
        [Test]
        public void GetJsonInt_ReadsRootProperty()
        {
            var node = NodeFactory.CreateNode(
                NodeFactory.GetNodePath().First(p => p.EndsWith("getJsonInt")));
            node.findPin("json").data = "{\"threshold\": 60, \"mode\": \"strict\"}";
            node.findPin("key").data = "threshold";
            node.excute(new NodeManager());

            Assert.AreEqual(60, node.findPin("result").data);
        }

        [Test]
        public void GetJsonString_ReadsRootProperty()
        {
            var node = NodeFactory.CreateNode(
                NodeFactory.GetNodePath().First(p => p.EndsWith("getJsonString")));
            node.findPin("json").data = "{\"threshold\": 60, \"mode\": \"strict\"}";
            node.findPin("key").data = "mode";
            node.excute(new NodeManager());

            Assert.AreEqual("strict", node.findPin("result").data);
        }

        [Test]
        public void GetJsonInt_ReturnsZero_WhenKeyMissing()
        {
            var node = NodeFactory.CreateNode(
                NodeFactory.GetNodePath().First(p => p.EndsWith("getJsonInt")));
            node.findPin("json").data = "{}";
            node.findPin("key").data = "threshold";
            node.excute(new NodeManager());

            Assert.AreEqual(0, node.findPin("result").data);
        }

        [Test]
        public void JsonNodes_AreDiscoveredViaReflection()
        {
            var paths = NodeFactory.GetNodePath();
            Assert.IsTrue(paths.Any(p => p.EndsWith("getJsonInt")));
            Assert.IsTrue(paths.Any(p => p.EndsWith("getJsonString")));
        }
    }
}
