using System;
using FlowNode;
using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class BranchNodeTests
    {
        [Test]
        public void ConditionPin_IsBoolTyped_WithDefaultValue()
        {
            var branch = new BranchNode();
            branch.init();

            var condition = branch.findPin("Condition");
            Assert.IsNotNull(condition);
            Assert.AreEqual(typeof(bool), condition.dataType, "Condition 引脚应为 bool 类型");
            Assert.AreEqual(false, condition.data, "Condition 引脚应有默认值 false");
        }

        [Test]
        public void BoolOutput_CanConnectToCondition()
        {
            var mgr = new NodeManager();
            var boolSource = new GetObjectNode("flag", typeof(bool));
            boolSource.init();
            var branch = new BranchNode();
            branch.init();
            mgr.addNode(boolSource);
            mgr.addNode(branch);

            mgr.addConnector(boolSource.findPin("flag"), branch.findPin("Condition"));

            Assert.AreEqual(1, mgr.getConnectors().Count);
        }

        [Test]
        public void IntOutput_CannotConnectToCondition()
        {
            var mgr = new NodeManager();
            var intSource = new GetObjectNode("n", typeof(int));
            intSource.init();
            var branch = new BranchNode();
            branch.init();
            mgr.addNode(intSource);
            mgr.addNode(branch);

            Assert.Throws<InvalidOperationException>(
                () => mgr.addConnector(intSource.findPin("n"), branch.findPin("Condition")));
        }

        [Test]
        public void GetDisplaySubtitle_ShowsConditionValue()
        {
            var branch = new BranchNode();
            branch.init();
            branch.pin_condition.data = true;
            Assert.AreEqual("true", branch.GetDisplaySubtitle());

            branch.pin_condition.data = false;
            Assert.AreEqual("false", branch.GetDisplaySubtitle());
        }
    }
}
