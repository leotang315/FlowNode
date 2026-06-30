using System;
using System.Collections.Generic;
using System.IO;
using FlowNode.app.serialization;
using FlowNode.hosting;

namespace FlowNode.HostDemo
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            var repoRoot = ResolveRepoRoot(args);
            var graphPath = Path.Combine(repoRoot, "samples", "score-check.xml");

            if (!File.Exists(graphPath))
            {
                Console.Error.WriteLine("[错误] 找不到示例图: " + graphPath);
                Console.Error.WriteLine("请先运行 scripts/generate-samples.ps1");
                return GraphRunner.ExitIo;
            }

            // 嵌入 FlowNode 的典型用法（约 5 行）：
            var host = new GraphHost();
            host.RegisterAssembly(typeof(HostDemoNodes).Assembly);
            var result = host.RunFile(graphPath, new GraphRunOptions
            {
                WorkingDirectory = repoRoot,
                Variables = new Dictionary<string, object>
                {
                    ["threshold"] = ParseThreshold(args, defaultValue: 60)
                }
            });

            foreach (var line in result.Logs)
                Console.WriteLine(line);
            foreach (var err in result.Errors)
                Console.Error.WriteLine("[错误] " + err);

            return result.ExitCode;
        }

        private static int ParseThreshold(string[] args, int defaultValue)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--threshold" &&
                    int.TryParse(args[i + 1], out int value))
                {
                    return value;
                }
            }

            return defaultValue;
        }

        private static string ResolveRepoRoot(string[] args)
        {
            if (args.Length > 0 && Directory.Exists(args[0]))
                return Path.GetFullPath(args[0]);

            return Path.GetFullPath(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
        }
    }
}
