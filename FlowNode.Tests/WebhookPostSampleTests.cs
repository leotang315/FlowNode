using System;
using System.IO;
using FlowNode.app.serialization;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class WebhookPostSampleTests
    {
        private static string RepoRoot => Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));

        [Test]
        public void GraphRunner_WebhookPostSample_CompletesAndWritesResponseFile()
        {
            var samplePath = Path.Combine(RepoRoot, "samples", "webhook-post.xml");
            Assert.IsTrue(File.Exists(samplePath), "缺少 samples/webhook-post.xml");

            var resultPath = Path.Combine(RepoRoot, "samples", "webhook-response.txt");
            if (File.Exists(resultPath))
                File.Delete(resultPath);

            try
            {
                var exitCode = GraphRunner.RunFile(
                    samplePath,
                    new GraphRunOptions { WorkingDirectory = RepoRoot });

                Assert.AreEqual(GraphRunner.ExitOk, exitCode);
                Assert.IsTrue(File.Exists(resultPath), "应写入 webhook-response.txt");

                var text = File.ReadAllText(resultPath);
                // 有网络时 httpbin 回显 POST body；无网络/限流时可能为空，流程仍应成功
                if (text.IndexOf("FlowNode", StringComparison.OrdinalIgnoreCase) >= 0)
                    Assert.IsTrue(text.TrimStart().StartsWith("{"), "httpbin 响应应为 JSON");
            }
            finally
            {
                if (File.Exists(resultPath))
                    File.Delete(resultPath);
            }
        }
    }
}
