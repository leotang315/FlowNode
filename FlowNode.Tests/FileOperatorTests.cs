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

        [Test]
        public void PathCombine_JoinsSegments()
        {
            var path = NodeFactory.GetNodePath().First(p => p.EndsWith("pathCombine"));
            var node = NodeFactory.CreateNode(path);
            node.findPin("a").data = "dir";
            node.findPin("b").data = "file.txt";
            node.excute(new NodeManager());

            StringAssert.Contains("file.txt", node.findPin("result").data as string);
        }

        [Test]
        public void ListFiles_ReturnsNewlineSeparatedPaths()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "flownode-list-" + Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            try
            {
                File.WriteAllText(Path.Combine(tempDir, "a.txt"), "a");
                File.WriteAllText(Path.Combine(tempDir, "b.txt"), "b");

                var path = NodeFactory.GetNodePath().First(p => p.EndsWith("listFiles"));
                var node = NodeFactory.CreateNode(path);
                node.findPin("directory").data = tempDir;
                node.excute(new NodeManager());

                var result = node.findPin("result").data as string;
                StringAssert.Contains("a.txt", result);
                StringAssert.Contains("b.txt", result);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }
    }

    [TestFixture]
    public class HttpOperatorTests
    {
        [Test]
        public void HttpGet_ReturnsEmpty_ForInvalidUrl()
        {
            var path = NodeFactory.GetNodePath().First(p => p.EndsWith("httpGet"));
            var node = NodeFactory.CreateNode(path);
            node.findPin("url").data = "not-a-valid-url";
            node.excute(new NodeManager());

            Assert.AreEqual(string.Empty, node.findPin("result").data);
        }

        [Test]
        public void HttpPost_ReturnsEmpty_ForInvalidUrl()
        {
            var path = NodeFactory.GetNodePath().First(p => p.EndsWith("httpPost"));
            var node = NodeFactory.CreateNode(path);
            node.findPin("url").data = "not-a-valid-url";
            node.findPin("body").data = "{\"ok\":true}";
            node.excute(new NodeManager());

            Assert.AreEqual(string.Empty, node.findPin("result").data);
        }

        [Test]
        public void HttpPost_ReturnsEmpty_WhenUrlMissing()
        {
            var path = NodeFactory.GetNodePath().First(p => p.EndsWith("httpPost"));
            var node = NodeFactory.CreateNode(path);
            node.findPin("url").data = "";
            node.findPin("body").data = "{}";
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
