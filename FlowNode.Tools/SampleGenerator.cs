using System;
using System.Collections.Generic;
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
            SaveConfigScoreCheck(Path.Combine(samplesDir, "config-score-check.xml"));

            File.WriteAllText(Path.Combine(samplesDir, "score-input.txt"), "85");
            File.WriteAllText(
                Path.Combine(samplesDir, "config.json"),
                "{\"threshold\": 60, \"mode\": \"strict\"}");

            return 0;
        }

        private static void SaveGraph(NodeManager mgr, string filePath, params NodeBase[] layoutOrder)
        {
            var layoutIndex = new Dictionary<INode, int>();
            for (int i = 0; i < layoutOrder.Length; i++)
                layoutIndex[layoutOrder[i]] = i;

            new NodeGraphSerializer(mgr).SaveToFile(filePath, node =>
            {
                if (!layoutIndex.TryGetValue(node, out int idx))
                    return null;

                return new NodeViewData
                {
                    X = 40 + idx * 240,
                    Y = 80,
                    Width = 200,
                    Height = 100
                };
            });
        }

        private static void SavePrintHello(string filePath)
        {
            var mgr = new NodeManager();
            var print = NodeFactory.CreateNode(
                NodeFactory.GetSystemNodePaths().First(p => p.EndsWith("Print")));
            print.findPin("Value").data = "hello-sample";
            mgr.addNode(print);
            SaveGraph(mgr, filePath, print);
        }

        private static void SaveWriteText(string filePath)
        {
            var mgr = new NodeManager();
            var write = new WriteTextNode();
            write.init();
            write.findPin("Path").data = "output.txt";
            write.findPin("Content").data = "written by FlowNode sample graph";
            mgr.addNode(write);
            SaveGraph(mgr, filePath, write);
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

            SaveGraph(mgr, filePath, read, concat, write);
        }

        /// <summary>
        /// 读分数 + 全局变量 threshold → 分支 → 写结果。
        /// CLI: FlowNode.Cli.exe --var threshold=60 samples/score-check.xml
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

            SaveGraph(mgr, filePath, read, toInt, getThreshold, compare, branch, writePass, writeFail);
        }

        /// <summary>
        /// 读 config.json 取 threshold + 读分数 → 分支 → 写结果（I3 JSON 示例）。
        /// CLI: FlowNode.Cli.exe samples/config-score-check.xml
        /// </summary>
        private static void SaveConfigScoreCheck(string filePath)
        {
            var mgr = new NodeManager();

            var readConfig = NodeFactory.CreateNode(
                NodeFactory.GetNodePath().First(p => p.EndsWith("readTextFile")));
            readConfig.findPin("path").data = "samples/config.json";

            var getThreshold = NodeFactory.CreateNode(
                NodeFactory.GetNodePath().First(p => p.EndsWith("getJsonInt")));
            getThreshold.findPin("key").data = "threshold";

            var readScore = NodeFactory.CreateNode(
                NodeFactory.GetNodePath().First(p => p.EndsWith("readTextFile")));
            readScore.findPin("path").data = "samples/score-input.txt";

            var toInt = NodeFactory.CreateNode(
                NodeFactory.GetNodePath().First(p => p.EndsWith("stringToInt")));

            var compare = NodeFactory.CreateNode(
                NodeFactory.GetNodePath().First(p => p.EndsWith("greaterOrEqual")));

            var branch = NodeFactory.CreateNode(
                NodeFactory.GetSystemNodePaths().First(p => p.EndsWith("Branch")));

            var writePath = NodeFactory.GetSystemNodePaths().First(p => p.EndsWith("WriteText"));
            var writePass = (WriteTextNode)NodeFactory.CreateNode(writePath);
            writePass.findPin("Path").data = "samples/config-score-result.txt";
            writePass.findPin("Content").data = "PASS: score meets config threshold";

            var writeFail = (WriteTextNode)NodeFactory.CreateNode(writePath);
            writeFail.findPin("Path").data = "samples/config-score-result.txt";
            writeFail.findPin("Content").data = "FAIL: score below config threshold";

            mgr.addNode(readConfig);
            mgr.addNode(getThreshold);
            mgr.addNode(readScore);
            mgr.addNode(toInt);
            mgr.addNode(compare);
            mgr.addNode(branch);
            mgr.addNode(writePass);
            mgr.addNode(writeFail);

            mgr.addConnector(readConfig.findPin("result"), getThreshold.findPin("json"));
            mgr.addConnector(getThreshold.findPin("result"), compare.findPin("b"));
            mgr.addConnector(readScore.findPin("result"), toInt.findPin("value"));
            mgr.addConnector(toInt.findPin("result"), compare.findPin("a"));
            mgr.addConnector(compare.findPin("result"), branch.findPin("Condition"));
            mgr.addConnector(branch.findPin("True"), writePass.findPin("Input"));
            mgr.addConnector(branch.findPin("False"), writeFail.findPin("Input"));

            SaveGraph(
                mgr,
                filePath,
                readConfig,
                getThreshold,
                readScore,
                toInt,
                compare,
                branch,
                writePass,
                writeFail);
        }
    }
}
