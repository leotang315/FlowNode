using System;
using System.IO;
using System.Linq;
using FlowNode.app.serialization;
using FlowNode.node;

namespace FlowNode.Tools
{
    /// <summary>生成 samples/ 下的示例图（由 scripts/generate-samples.ps1 调用）。</summary>
    internal static class SampleGenerator
    {
        public static int Main(string[] args)
        {
            var repoRoot = args.Length > 0
                ? args[0]
                : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));

            var samplesDir = Path.Combine(repoRoot, "samples");
            Directory.CreateDirectory(samplesDir);

            SavePrintHello(Path.Combine(samplesDir, "print-hello.xml"));
            SaveWriteText(Path.Combine(samplesDir, "write-text.xml"));

            return 0;
        }

        private static void SavePrintHello(string filePath)
        {
            var mgr = new NodeManager();
            var print = NodeFactory.CreateNode(
                NodeFactory.GetSystemNodePaths().First(p => p.EndsWith("Print")));
            print.findPin("Value").data = "hello-sample";
            mgr.addNode(print);
            new NodeGraphSerializer(mgr).SaveToFile(filePath);
        }

        private static void SaveWriteText(string filePath)
        {
            var mgr = new NodeManager();
            var write = new WriteTextNode();
            write.init();
            write.findPin("Path").data = "output.txt";
            write.findPin("Content").data = "written by FlowNode sample graph";
            mgr.addNode(write);
            new NodeGraphSerializer(mgr).SaveToFile(filePath);
        }
    }
}
