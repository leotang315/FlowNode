using System.IO;
using System.Linq;
using FlowNode.app.serialization;
using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class FileOperatorTests
    {
        [Test]
        public void ReadTextFile_ReturnsContent_WhenFileExists()
        {
            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(temp, "line1");

                var path = NodeFactory.GetNodePath().First(p => p.EndsWith("readTextFile"));
                var node = NodeFactory.CreateNode(path);
                node.findPin("path").data = temp;
                node.excute(new NodeManager());

                Assert.AreEqual("line1", node.findPin("result").data);
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void ReadTextFile_ReturnsEmpty_WhenMissing()
        {
            var path = NodeFactory.GetNodePath().First(p => p.EndsWith("readTextFile"));
            var node = NodeFactory.CreateNode(path);
            node.findPin("path").data = Path.Combine(Path.GetTempPath(), "flownode-missing-" + Path.GetRandomFileName());
            node.excute(new NodeManager());

            Assert.AreEqual(string.Empty, node.findPin("result").data);
        }
    }

    [TestFixture]
    public class WriteTextNodeTests
    {
        [Test]
        public void WriteText_WritesFileOnExecute()
        {
            var temp = Path.Combine(Path.GetTempPath(), "flownode-write-" + Path.GetRandomFileName() + ".txt");
            try
            {
                var write = new WriteTextNode();
                write.init();
                write.findPin("Path").data = temp;
                write.findPin("Content").data = "saved";

                var mgr = new NodeManager();
                mgr.addNode(write);
                mgr.run();

                Assert.IsTrue(File.Exists(temp));
                Assert.AreEqual("saved", File.ReadAllText(temp));
            }
            finally
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
        }

        [Test]
        public void WriteText_AppendsWhenAppendTrue()
        {
            var temp = Path.Combine(Path.GetTempPath(), "flownode-append-" + Path.GetRandomFileName() + ".txt");
            try
            {
                File.WriteAllText(temp, "a");

                var write = new WriteTextNode { Append = true };
                write.init();
                write.findPin("Path").data = temp;
                write.findPin("Content").data = "b";

                var mgr = new NodeManager();
                mgr.addNode(write);
                mgr.run();

                Assert.AreEqual("ab", File.ReadAllText(temp));
            }
            finally
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
        }
    }

    [TestFixture]
    public class IoGraphExecutionTests
    {
        [Test]
        public void ReadThenPrint_LogsFileContent()
        {
            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(temp, "from-disk");

                var mgr = new NodeManager();
                var logs = new System.Collections.Generic.List<string>();
                mgr.Log += logs.Add;

                var readPath = NodeFactory.GetNodePath().First(p => p.EndsWith("readTextFile"));
                var read = NodeFactory.CreateNode(readPath);
                read.findPin("path").data = temp;

                var printPath = NodeFactory.GetSystemNodePaths().First(p => p.EndsWith("Print"));
                var print = NodeFactory.CreateNode(printPath);

                mgr.addNode(read);
                mgr.addNode(print);
                mgr.addConnector(read.findPin("result"), print.findPin("Value"));

                mgr.run();

                Assert.IsTrue(logs.Any(l => l.Contains("from-disk")));
            }
            finally
            {
                File.Delete(temp);
            }
        }
    }
}
