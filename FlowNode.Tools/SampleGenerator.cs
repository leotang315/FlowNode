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
            SaveReadTransformWrite(Path.Combine(samplesDir, "read-transform-write.xml"));
            SaveScoreCheck(Path.Combine(samplesDir, "score-check.xml"));
            File.WriteAllText(Path.Combine(samplesDir, "score-input.txt"), "85");

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

        private static void SaveReadTransformWrite(string filePath)
        {
            var mgr = new NodeManager();

            var read = NodeFactory.CreateNode(
                NodeFactory.GetNodePath().First(p => p.EndsWith("readTextFile")));
            read.findPin("path").data = "input.txt";

            var concat = NodeFactory.CreateNode(
                NodeFactory.GetNodePath().First(p => p.EndsWith("concat")));
            concat.findPin("b").data = " [processed]";

            var write = new WriteTextNode();
            write.init();
            write.findPin("Path").data = "processed-output.txt";

            mgr.addNode(read);
            mgr.addNode(concat);
            mgr.addNode(write);
            mgr.addConnector(read.findPin("result"), concat.findPin("a"));
            mgr.addConnector(concat.findPin("result"), write.findPin("Content"));

            new NodeGraphSerializer(mgr).SaveToFile(filePath);
        }

        /// <summary>
        /// 业务小场景：读分数文件 → 与阈值比较 → 分支 → 写结果文件。
        /// 运行：FlowNode.Cli.exe --var threshold=60 samples/score-check.xml
        /// </summary>
        private static void SaveScoreCheck(string filePath)
        {
            var mgr = new NodeManager();

            var read = NodeFactory.CreateNode(
                NodeFactory.GetNodePath().First(p => p.EndsWith("readTextFile")));
            read.findPin("path").data = "samples/score-input.txt";

            var toInt = NodeFactory.CreateNode(
                NodeFactory.GetNodePath().First(p => p.EndsWith("stringToInt")));

            var getThreshold = NodeFactory.CreateVarNode("threshold", typeof(int), isSet: false);

            var compare = NodeFactory.CreateNode(
                NodeFactory.GetNodePath().First(p => p.EndsWith("greaterOrEqual")));

            var branch = NodeFactory.CreateNode(
                NodeFactory.GetSystemNodePaths().First(p => p.EndsWith("Branch")));

            var writePath = NodeFactory.GetSystemNodePaths().First(p => p.EndsWith("WriteText"));
            var writePass = (WriteTextNode)NodeFactory.CreateNode(writePath);
            writePass.findPin("Path").data = "samples/score-result.txt";
            writePass.findPin("Content").data = "PASS: score meets threshold";

            var writeFail = (WriteTextNode)NodeFactory.CreateNode(writePath);
            writeFail.findPin("Path").data = "samples/score-result.txt";
            writeFail.findPin("Content").data = "FAIL: score below threshold";

            mgr.addNode(read);
            mgr.addNode(toInt);
            mgr.addNode(getThreshold);
            mgr.addNode(compare);
            mgr.addNode(branch);
            mgr.addNode(writePass);
            mgr.addNode(writeFail);

            mgr.addConnector(read.findPin("result"), toInt.findPin("value"));
            mgr.addConnector(toInt.findPin("result"), compare.findPin("a"));
            mgr.addConnector(getThreshold.findPin("threshold"), compare.findPin("b"));
            mgr.addConnector(compare.findPin("result"), branch.findPin("Condition"));
            mgr.addConnector(branch.findPin("True"), writePass.findPin("Input"));
            mgr.addConnector(branch.findPin("False"), writeFail.findPin("Input"));

            mgr.SetDataObject("threshold", 60, typeof(int));

            new NodeGraphSerializer(mgr).SaveToFile(filePath);
        }
    }
}
