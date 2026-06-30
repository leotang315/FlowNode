using System;
using System.Collections.Generic;
using System.IO;
using FlowNode.app.serialization;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class ConfigScoreCheckSampleTests
    {
        private static string RepoRoot => Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));

        [Test]
        public void GraphRunner_ConfigScoreCheck_WritesPassFromJsonThreshold()
        {
            var samplePath = Path.Combine(RepoRoot, "samples", "config-score-check.xml");
            Assert.IsTrue(File.Exists(samplePath), "缺少 samples/config-score-check.xml");

            var resultPath = Path.Combine(RepoRoot, "samples", "config-score-result.txt");
            if (File.Exists(resultPath))
                File.Delete(resultPath);

            try
            {
                var exitCode = GraphRunner.RunFile(
                    samplePath,
                    new GraphRunOptions { WorkingDirectory = RepoRoot });

                Assert.AreEqual(GraphRunner.ExitOk, exitCode);
                Assert.IsTrue(File.Exists(resultPath));
                Assert.IsTrue(
                    File.ReadAllText(resultPath).StartsWith("PASS", StringComparison.Ordinal));
            }
            finally
            {
                if (File.Exists(resultPath))
                    File.Delete(resultPath);
            }
        }
    }
}
