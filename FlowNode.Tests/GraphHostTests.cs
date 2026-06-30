using System.IO;
using System.Linq;
using FlowNode.app.serialization;
using FlowNode.hosting;
using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class GraphHostTests
    {
        [Test]
        public void GraphHost_RunsSamplePrintGraph()
        {
            var samplePath = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..", "samples", "print-hello.xml"));

            Assert.IsTrue(File.Exists(samplePath), "缺少 samples/print-hello.xml");

            var host = new GraphHost();
            host.RegisterAssembly(typeof(FileOperator).Assembly);

            var result = host.RunFile(samplePath);
            Assert.IsTrue(result.Success, string.Join("; ", result.Errors));
            Assert.IsTrue(result.Logs.Any(l => l.Contains("hello-sample")));
        }

        [Test]
        public void GraphRunOptions_OverridesVariableAfterLoad()
        {
            var mgr = new NodeManager();
            mgr.SetDataObject("msg", "from-xml", typeof(string));

            var options = new GraphRunOptions
            {
                Variables = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["msg"] = "from-cli"
                }
            };
            options.ApplyTo(mgr);

            Assert.AreEqual("from-cli", mgr.GetDataObject("msg"));
        }
    }
}
