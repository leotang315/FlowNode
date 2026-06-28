using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    /// <summary>用于统计执行次数的简单循环体节点（仅测试使用）。</summary>
    internal class CountingNode : NodeBase
    {
        public int Count;

        public override void allocateDefaultPins()
        {
            createPin("In", PinDirection.Input, PinType.Execute);
        }

        public override void excute(INodeManager manager)
        {
            Count++;
        }
    }

    [TestFixture]
    public class LoopNodeTests
    {
        [Test]
        public void Loop_RunsBody_LoopCountTimes()
        {
            var mgr = new NodeManager();
            var loop = new LoopNode();
            loop.init();
            loop.LoopCount = 3;
            var body = new CountingNode();
            body.init();

            mgr.addNode(loop);
            mgr.addNode(body);
            mgr.addConnector(loop.findPin("LoopBody"), body.findPin("In"));

            mgr.run();

            Assert.AreEqual(3, body.Count);
        }

        [Test]
        public void Loop_RerunResetsCounter()
        {
            var mgr = new NodeManager();
            var loop = new LoopNode();
            loop.init();
            loop.LoopCount = 2;
            var body = new CountingNode();
            body.init();

            mgr.addNode(loop);
            mgr.addNode(body);
            mgr.addConnector(loop.findPin("LoopBody"), body.findPin("In"));

            mgr.run();
            mgr.run();

            // 第二次运行若计数器未归零，循环体会被跳过（Count 仍为 2）
            Assert.AreEqual(4, body.Count, "重复运行应能再次循环，证明内部计数器已归零");
        }

        [Test]
        public void LoopCount_IsAPublicProperty_ForGridAndSerialization()
        {
            // 属性面板(PropertyGrid)与序列化只识别公有属性，故 LoopCount 必须是属性而非字段
            var prop = typeof(LoopNode).GetProperty("LoopCount");
            Assert.IsNotNull(prop, "LoopCount 应为公有属性");
            Assert.IsTrue(prop.CanRead && prop.CanWrite);
        }
    }
}
