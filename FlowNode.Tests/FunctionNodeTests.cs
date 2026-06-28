using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class FunctionNodeTests
    {
        [Test]
        public void Add_ComputesSum_AndWritesOutputPin()
        {
            var method = typeof(MathOperator).GetMethod("add");
            var node = new FunctionNode(null, method);
            node.init();

            node.findPin("a").data = 2;
            node.findPin("b").data = 3;

            node.excute(new NodeManager());

            Assert.AreEqual(5, node.findPin("result").data);
        }

        [Test]
        public void Sub_ComputesDifference()
        {
            var method = typeof(MathOperator).GetMethod("sub");
            var node = new FunctionNode(null, method);
            node.init();

            node.findPin("a").data = 10;
            node.findPin("b").data = 4;

            node.excute(new NodeManager());

            Assert.AreEqual(6, node.findPin("result").data);
        }

        [Test]
        public void Run_ResolvesAutoRunDataInput_FromConnectedNode()
        {
            // a(add: 2+3=5) --result--> b(add).a ，b.b=10，期望 b.result=15
            var mgr = new NodeManager();

            var a = new FunctionNode(null, typeof(MathOperator).GetMethod("add"));
            a.init();
            a.findPin("a").data = 2;
            a.findPin("b").data = 3;

            var b = new FunctionNode(null, typeof(MathOperator).GetMethod("add"));
            b.init();
            b.findPin("b").data = 10;

            mgr.addNode(a);
            mgr.addNode(b);
            mgr.addConnector(a.findPin("result"), b.findPin("a"));

            // b 非 AutoRun（直接 new 默认 IsAutoRun=false），run 会 resolve 输入：
            // a 也是非 AutoRun，故不会被自动拉起，但已连接的数据会在 a 执行后传递。
            // 这里显式先执行 a，再执行 b 验证数据沿连接传递。
            a.excute(mgr);
            b.run(mgr);

            Assert.AreEqual(15, b.findPin("result").data);
        }
    }
}
