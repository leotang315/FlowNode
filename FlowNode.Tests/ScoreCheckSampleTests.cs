using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowNode.app.serialization;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class ScoreCheckSampleTests
    {
        private static string RepoRoot => Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));

        [Test]
        public void GraphRunner_ScoreCheckSample_WritesPassWhenAboveThreshold()
        {
            var samplePath = Path.Combine(RepoRoot, "samples", "score-check.xml");
            Assert.IsTrue(File.Exists(samplePath), "缺少 samples/score-check.xml，请运行 generate-samples.ps1");

            var scoreInput = Path.Combine(RepoRoot, "samples", "score-input.txt");
            Assert.IsTrue(File.Exists(scoreInput), "缺少 samples/score-input.txt");

            var resultPath = Path.Combine(RepoRoot, "samples", "score-result.txt");
            if (File.Exists(resultPath))
                File.Delete(resultPath);

            try
            {
                var options = new GraphRunOptions
                {
                    WorkingDirectory = RepoRoot,
                    Variables = new Dictionary<string, object> { ["threshold"] = 60 }
                };

                var exitCode = GraphRunner.RunFile(samplePath, options);

                Assert.AreEqual(GraphRunner.ExitOk, exitCode);
                Assert.IsTrue(File.Exists(resultPath), "应写入 score-result.txt");

                var text = File.ReadAllText(resultPath);
                Assert.IsTrue(text.StartsWith("PASS", StringComparison.Ordinal),
                    "score-input.txt 为 85，threshold=60 应走 PASS 分支");
            }
            finally
            {
                if (File.Exists(resultPath))
                    File.Delete(resultPath);
            }
        }

        [Test]
        public void GraphRunner_ScoreCheckSample_WritesFailWhenBelowThreshold()
        {
            var samplePath = Path.Combine(RepoRoot, "samples", "score-check.xml");
            var resultPath = Path.Combine(RepoRoot, "samples", "score-result.txt");
            if (File.Exists(resultPath))
                File.Delete(resultPath);

            try
            {
                var options = new GraphRunOptions
                {
                    WorkingDirectory = RepoRoot,
                    Variables = new Dictionary<string, object> { ["threshold"] = 90 }
                };

                var exitCode = GraphRunner.RunFile(samplePath, options);

                Assert.AreEqual(GraphRunner.ExitOk, exitCode);
                var text = File.ReadAllText(resultPath);
                Assert.IsTrue(text.StartsWith("FAIL", StringComparison.Ordinal),
                    "threshold=90 时 85 分应走 FAIL 分支");
            }
            finally
            {
                if (File.Exists(resultPath))
                    File.Delete(resultPath);
            }
        }
    }
}
