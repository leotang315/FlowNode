using System.Linq;
using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class NodePropertySheetTests
    {
        [Test]
        public void FunctionNode_ExposesInputPins_InPropertySheet()
        {
            var addPath = NodeFactory.GetFunctionNodePaths().First(p => p.EndsWith("add"));
            var add = NodeFactory.CreateNode(addPath);
            var sheet = new NodePropertySheet(add);

            var props = sheet.GetProperties();
            var aProp = props.Find("a", false);
            var bProp = props.Find("b", false);

            Assert.IsNotNull(aProp, "属性面板应暴露 add 的 a 引脚");
            Assert.IsNotNull(bProp, "属性面板应暴露 add 的 b 引脚");

            aProp.SetValue(add, 3);
            bProp.SetValue(add, 4);
            Assert.AreEqual(3, add.findPin("a").data);
            Assert.AreEqual(4, add.findPin("b").data);

            add.excute(new NodeManager());
            Assert.AreEqual(7, add.findPin("result").data);
        }

        [Test]
        public void Constant_ValueSetter_SyncsOutputPin()
        {
            var node = new IntConstantNode();
            node.init();
            node.Value = 99;
            Assert.AreEqual(99, node.findPin("Value").data);
        }

        [Test]
        public void Constant_Value_ExposedInPropertySheet()
        {
            var node = new IntConstantNode();
            node.init();
            var sheet = new NodePropertySheet(node);
            var valueProp = sheet.GetProperties().Find("Value", false);
            Assert.IsNotNull(valueProp);
            valueProp.SetValue(node, 42);
            Assert.AreEqual(42, node.Value);
            Assert.AreEqual(42, node.findPin("Value").data);
        }

        [Test]
        public void GetObjectNode_ExposesGlobalVariableValue_InPropertySheet()
        {
            var mgr = new NodeManager();
            mgr.SetDataObject("threshold", 60, typeof(int));

            var get = (GetObjectNode)NodeFactory.CreateVarNode("threshold", typeof(int), isSet: false);

            var sheet = new NodePropertySheet(get, mgr);
            var valueProp = sheet.GetProperties().Find("Value", false);
            Assert.IsNotNull(valueProp, "Get 节点应在属性面板暴露全局变量 Value");

            Assert.AreEqual(60, valueProp.GetValue(get));

            valueProp.SetValue(get, 90);
            Assert.AreEqual(90, mgr.GetDataObject("threshold"));
            get.RefreshOutputFrom(mgr);
            Assert.AreEqual("90", get.GetDisplaySubtitle());
        }
    }
}
