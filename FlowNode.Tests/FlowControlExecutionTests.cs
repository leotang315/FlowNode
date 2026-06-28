using System.Collections.Generic;
using System.Linq;
using FlowNode;
using FlowNode.app.serialization;
using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    /// <summary>执行流测试辅助：透传执行引脚。</summary>
    internal class PassThroughNode : NodeBase
    {
        public string Label { get; set; }
        public List<string> ExecutionLog { get; set; }

        public override void allocateDefaultPins()
        {
            createPin("Input", PinDirection.Input, PinType.Execute);
            createPin("Output", PinDirection.Output, PinType.Execute);
        }

        public override void excute(INodeManager manager)
        {
            ExecutionLog?.Add(Label ?? Name);
            manager.pushNextConnectNode(findPin("Output"));
        }
    }

    /// <summary>记录每次迭代读到的 Index 值。</summary>
    internal class IndexCaptureNode : NodeBase
    {
        public List<int> Captured = new List<int>();

        public override void allocateDefaultPins()
        {
            createPin("Input", PinDirection.Input, PinType.Execute);
            createPin("Index", PinDirection.Input, PinType.Data, typeof(int), 0);
        }

        public override void excute(INodeManager manager)
        {
            Captured.Add((int)findPin("Index").data);
        }
    }

    [TestFixture]
    public class FlowControlExecutionTests
    {
        private static List<string> CollectPrintLogs(NodeManager mgr)
        {
            var logs = new List<string>();
            mgr.Log += s =>
            {
                if (s != null && s.StartsWith("[Print]"))
                    logs.Add(s);
            };
            return logs;
        }

        [Test]
        public void Branch_TrueCondition_RunsTrueBranchOnly()
        {
            var mgr = new NodeManager();
            var logs = CollectPrintLogs(mgr);

            var branch = new BranchNode();
            branch.init();
            branch.findPin("Condition").data = true;

            var printTrue = new PrintNode();
            printTrue.init();
            var printFalse = new PrintNode();
            printFalse.init();

            mgr.addNode(branch);
            mgr.addNode(printTrue);
            mgr.addNode(printFalse);
            mgr.addConnector(branch.findPin("True"), printTrue.findPin("Input"));
            mgr.addConnector(branch.findPin("False"), printFalse.findPin("Input"));

            mgr.run();

            Assert.AreEqual(1, logs.Count, "Condition=true 时应只执行 True 分支上的 Print");
        }

        [Test]
        public void Branch_FalseCondition_RunsFalseBranchOnly()
        {
            var mgr = new NodeManager();
            var logs = CollectPrintLogs(mgr);

            var branch = new BranchNode();
            branch.init();
            branch.findPin("Condition").data = false;

            var printTrue = new PrintNode();
            printTrue.init();
            var printFalse = new PrintNode();
            printFalse.init();

            mgr.addNode(branch);
            mgr.addNode(printTrue);
            mgr.addNode(printFalse);
            mgr.addConnector(branch.findPin("True"), printTrue.findPin("Input"));
            mgr.addConnector(branch.findPin("False"), printFalse.findPin("Input"));

            mgr.run();

            Assert.AreEqual(1, logs.Count, "Condition=false 时应只执行 False 分支上的 Print");
        }

        [Test]
        public void Branch_WithCompare_RoutesToTrueWhenGreater()
        {
            var mgr = new NodeManager();
            var logs = CollectPrintLogs(mgr);

            var five = new IntConstantNode();
            five.init();
            five.Value = 5;
            var three = new IntConstantNode();
            three.init();
            three.Value = 3;

            var cmp = new FunctionNode(null, typeof(ComparisonOperator).GetMethod("Greater"));
            cmp.init();

            var branch = new BranchNode();
            branch.init();
            var printTrue = new PrintNode();
            printTrue.init();
            var printFalse = new PrintNode();
            printFalse.init();

            mgr.addNode(five);
            mgr.addNode(three);
            mgr.addNode(cmp);
            mgr.addNode(branch);
            mgr.addNode(printTrue);
            mgr.addNode(printFalse);

            mgr.addConnector(five.findPin("Value"), cmp.findPin("a"));
            mgr.addConnector(three.findPin("Value"), cmp.findPin("b"));
            mgr.addConnector(cmp.findPin("result"), branch.findPin("Condition"));
            mgr.addConnector(branch.findPin("True"), printTrue.findPin("Input"));
            mgr.addConnector(branch.findPin("False"), printFalse.findPin("Input"));

            mgr.run();

            Assert.AreEqual(1, logs.Count, "5>3 应走 True 分支");
        }

        [Test]
        public void Loop_IndexPin_ExposesZeroBasedIteration()
        {
            var mgr = new NodeManager();
            var loop = new LoopNode();
            loop.init();
            loop.LoopCount = 3;

            var body = new IndexCaptureNode();
            body.init();

            mgr.addNode(loop);
            mgr.addNode(body);
            mgr.addConnector(loop.findPin("LoopBody"), body.findPin("Input"));
            mgr.addConnector(loop.findPin("Index"), body.findPin("Index"));

            mgr.run();

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, body.Captured);
        }

        [Test]
        public void Loop_CompletedPin_RunsAfterAllIterations()
        {
            var mgr = new NodeManager();
            var loop = new LoopNode();
            loop.init();
            loop.LoopCount = 2;

            var body = new CountingNode();
            body.init();
            var completed = new CountingNode();
            completed.init();

            mgr.addNode(loop);
            mgr.addNode(body);
            mgr.addNode(completed);
            mgr.addConnector(loop.findPin("LoopBody"), body.findPin("In"));
            mgr.addConnector(loop.findPin("Completed"), completed.findPin("In"));

            mgr.run();

            Assert.AreEqual(2, body.Count);
            Assert.AreEqual(1, completed.Count, "循环结束后 Completed 引脚应对下游执行一次");
        }

        [Test]
        public void Sequence_RunsDownstreamInOrder()
        {
            var mgr = new NodeManager();
            var order = new List<string>();

            var seq = new SequenceNode();
            seq.init();

            var a = new PassThroughNode { Name = "A", Label = "A", ExecutionLog = order };
            a.init();
            var b = new PassThroughNode { Name = "B", Label = "B", ExecutionLog = order };
            b.init();
            var c = new PassThroughNode { Name = "C", Label = "C", ExecutionLog = order };
            c.init();

            mgr.addNode(seq);
            mgr.addNode(a);
            mgr.addNode(b);
            mgr.addNode(c);
            mgr.addConnector(seq.findPin("Output"), a.findPin("Input"));
            mgr.addConnector(a.findPin("Output"), b.findPin("Input"));
            mgr.addConnector(b.findPin("Output"), c.findPin("Input"));

            mgr.run();

            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, order);
        }

        [Test]
        public void SnapshotApply_PreservesGraphExecutionResult()
        {
            var start = new PassThroughNode { Name = "Start" };
            start.init();

            var add = new FunctionNode(null, typeof(MathOperator).GetMethod("add"));
            add.IsAutoRun = false;
            add.init();
            add.findPin("a").data = 10;
            add.findPin("b").data = 5;

            var print = new PrintNode();
            print.init();

            var mgr = new NodeManager();
            var logs = new List<string>();
            mgr.Log += s => { if (s != null && s.StartsWith("[Print]")) logs.Add(s); };

            mgr.addNode(start);
            mgr.addNode(add);
            mgr.addNode(print);
            mgr.addConnector(start.findPin("Output"), add.findPin("Input"));
            mgr.addConnector(add.findPin("Output"), print.findPin("Input"));
            mgr.addConnector(add.findPin("result"), print.findPin("Value"));

            var props = NodeSnapshotHelper.CaptureProperties(add);
            var pins = NodeSnapshotHelper.CapturePins(add);

            var mgr2 = new NodeManager();
            var logs2 = new List<string>();
            mgr2.Log += s => { if (s != null && s.StartsWith("[Print]")) logs2.Add(s); };

            var start2 = new PassThroughNode { Name = "Start" };
            start2.init();
            var add2 = new FunctionNode(null, typeof(MathOperator).GetMethod("add"));
            add2.IsAutoRun = false;
            add2.init();
            NodeSnapshotHelper.Apply(add2, props, pins);
            var print2 = new PrintNode();
            print2.init();

            mgr2.addNode(start2);
            mgr2.addNode(add2);
            mgr2.addNode(print2);
            mgr2.addConnector(start2.findPin("Output"), add2.findPin("Input"));
            mgr2.addConnector(add2.findPin("Output"), print2.findPin("Input"));
            mgr2.addConnector(add2.findPin("result"), print2.findPin("Value"));

            mgr.run();
            mgr2.run();

            Assert.AreEqual(logs, logs2, "快照恢复后的图应与原图产生相同执行输出");
            Assert.AreEqual("[Print] 15", logs[0]);
        }
    }
}
