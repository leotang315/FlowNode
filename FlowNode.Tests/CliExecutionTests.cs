using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowNode.app.serialization;
using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class CliExecutionTests
    {
        [Test]
        public void GraphRunner_RunsSavedPrintGraph()
        {
            var temp = Path.GetTempFileName();
            try
            {
                BuildPrintGraph(temp);

                var logs = new List<string>();
                var exitCode = GraphRunner.RunFile(temp, log: logs.Add);

                Assert.AreEqual(GraphRunner.ExitOk, exitCode);
                Assert.IsTrue(logs.Any(l => l != null && l.Contains("hello-cli")),
                    "Print 节点应把值写入日志");
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void GraphRunner_ReturnsValidationError_ForEmptyGraph()
        {
            var temp = Path.GetTempFileName();
            try
            {
                var mgr = new NodeManager();
                new NodeGraphSerializer(mgr).SaveToFile(temp);

                var errors = new List<string>();
                var exitCode = GraphRunner.RunFile(temp, error: errors.Add);

                Assert.AreEqual(GraphRunner.ExitValidation, exitCode);
                Assert.IsTrue(errors.Count > 0);
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void GraphRunner_ReturnsIoError_WhenFileMissing()
        {
            var exitCode = GraphRunner.RunFile(
                Path.Combine(Path.GetTempPath(), "flownode-missing-" + Path.GetRandomFileName() + ".xml"));

            Assert.AreEqual(GraphRunner.ExitIo, exitCode);
        }

        private static void BuildPrintGraph(string filePath)
        {
            var mgr = new NodeManager();
            var printPath = NodeFactory.GetSystemNodePaths().First(p => p.EndsWith("Print"));
            var print = NodeFactory.CreateNode(printPath);
            print.findPin("Value").data = "hello-cli";
            mgr.addNode(print);

            new NodeGraphSerializer(mgr).SaveToFile(filePath);
        }
    }
}
