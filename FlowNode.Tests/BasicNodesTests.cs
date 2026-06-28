using System;
using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class BasicNodesTests
    {
        [Test]
        public void IntConstant_OutputsItsValue()
        {
            var c = new IntConstantNode();
            c.init();
            c.Value = 42;

            c.excute(new NodeManager());

            Assert.AreEqual(42, c.findPin("Value").data);
        }

        [Test]
        public void Constant_ValueIsAPublicProperty_ForGridAndSerialization()
        {
            var prop = typeof(IntConstantNode).GetProperty("Value");
            Assert.IsNotNull(prop, "常量节点的 Value 必须是公有属性（属性面板/序列化只识别属性）");
            Assert.IsTrue(prop.CanRead && prop.CanWrite);
        }

        [Test]
        public void Print_WritesValueToLog()
        {
            var mgr = new NodeManager();
            string captured = null;
            mgr.Log += s => captured = s;

            var print = new PrintNode();
            print.init();
            print.findPin("Value").data = "hello";

            print.excute(mgr);

            Assert.AreEqual("[Print] hello", captured);
        }

        [Test]
        public void Print_WritesNull_WhenNoValue()
        {
            var mgr = new NodeManager();
            string captured = null;
            mgr.Log += s => captured = s;

            var print = new PrintNode();
            print.init();

            print.excute(mgr);

            Assert.AreEqual("[Print] null", captured);
        }

        [Test]
        public void IntConstant_CanConnectToPrint_ObjectInput()
        {
            var mgr = new NodeManager();
            var c = new IntConstantNode();
            c.init();
            var print = new PrintNode();
            print.init();
            mgr.addNode(c);
            mgr.addNode(print);

            // int 输出可连到 object 类型的 Value 输入
            mgr.addConnector(c.findPin("Value"), print.findPin("Value"));

            Assert.AreEqual(1, mgr.getConnectors().Count);
        }

        [Test]
        public void IntConstant_GetDisplaySubtitle_ShowsCurrentValue()
        {
            var c = new IntConstantNode();
            c.init();
            c.Value = 42;
            Assert.AreEqual("42", c.GetDisplaySubtitle());
        }

        [Test]
        public void CommentNode_GetDisplaySubtitle_ShowsText()
        {
            var node = new CommentNode();
            node.init();
            node.Text = "checkpoint";
            Assert.AreEqual("checkpoint", node.GetDisplaySubtitle());
        }

        [Test]
        public void CommentNode_PassesExecuteThrough()
        {
            var mgr = new NodeManager();
            var comment = new CommentNode();
            comment.init();
            var print = new PrintNode();
            print.init();
            mgr.addNode(comment);
            mgr.addNode(print);
            mgr.addConnector(comment.findPin("Output"), print.findPin("Input"));

            string log = null;
            mgr.Log += s => { if (s != null && s.StartsWith("[Print]")) log = s; };

            mgr.run();
            Assert.AreEqual("[Print] null", log);
        }

        [Test]
        public void ConstantToPrint_Flow_LogsResolvedValue()
        {
            var mgr = new NodeManager();
            string printed = null;
            mgr.Log += s => { if (s != null && s.StartsWith("[Print]")) printed = s; };

            var c = new IntConstantNode();
            c.init();
            c.Value = 7;
            var print = new PrintNode();
            print.init();

            mgr.addNode(c);
            mgr.addNode(print);
            mgr.addConnector(c.findPin("Value"), print.findPin("Value"));

            mgr.run();

            Assert.AreEqual("[Print] 7", printed);
        }
    }
}
