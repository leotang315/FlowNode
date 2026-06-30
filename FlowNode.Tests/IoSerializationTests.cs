using System.IO;
using System.Linq;
using FlowNode.app.serialization;
using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class IoSerializationTests
    {
        [Test]
        public void SaveLoad_RoundTrips_WriteTextAppendProperty()
        {
            var mgr = new NodeManager();
            var writePath = NodeFactory.GetSystemNodePaths().First(p => p.EndsWith("WriteText"));
            var write = (WriteTextNode)NodeFactory.CreateNode(writePath);
            write.Append = true;
            write.findPin("Path").data = "out.txt";
            mgr.addNode(write);

            var temp = Path.GetTempFileName();
            try
            {
                new NodeGraphSerializer(mgr).SaveToFile(temp);

                var mgr2 = new NodeManager();
                new NodeGraphSerializer(mgr2).LoadFromFile(temp);

                var loaded = (WriteTextNode)mgr2.getNodes().Single();
                Assert.IsTrue(loaded.Append);
                Assert.AreEqual("out.txt", loaded.findPin("Path").data);
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void SaveLoad_RoundTrips_ReadTextFilePinDefault()
        {
            var mgr = new NodeManager();
            var path = NodeFactory.GetNodePath().First(p => p.EndsWith("readTextFile"));
            var node = NodeFactory.CreateNode(path);
            node.findPin("path").data = @"C:\data\input.txt";
            mgr.addNode(node);

            var temp = Path.GetTempFileName();
            try
            {
                new NodeGraphSerializer(mgr).SaveToFile(temp);

                var mgr2 = new NodeManager();
                new NodeGraphSerializer(mgr2).LoadFromFile(temp);

                var loaded = mgr2.getNodes().Single();
                Assert.AreEqual(@"C:\data\input.txt", loaded.findPin("path").data as string);
            }
            finally
            {
                File.Delete(temp);
            }
        }
    }
}
