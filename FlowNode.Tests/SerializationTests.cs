using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowNode.app.serialization;
using FlowNode.app.view;
using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class SerializationTests
    {
        private static NodeSerializationService NewService(out NodeManager mgr)
        {
            mgr = new NodeManager();
            var views = new Dictionary<INode, NodeView>();
            return new NodeSerializationService(mgr, views);
        }

        [Test]
        public void SaveLoad_RoundTrips_NodesConnectorsAndPinDefaults()
        {
            var svc = NewService(out var mgr);

            var addPath = NodeFactory.GetFunctionNodePaths().First(p => p.EndsWith("add"));
            var add = NodeFactory.CreateNode(addPath);
            add.findPin("a").data = 7;
            add.findPin("b").data = 5;
            mgr.addNode(add);

            var printPath = NodeFactory.GetSystemNodePaths().First(p => p.EndsWith("Print"));
            var print = NodeFactory.CreateNode(printPath);
            mgr.addNode(print);

            mgr.addConnector(add.findPin("result"), print.findPin("Value"));

            var temp = Path.GetTempFileName();
            try
            {
                svc.SaveToFile(temp);

                var svc2 = NewService(out var mgr2);
                svc2.LoadFromFile(temp);

                Assert.AreEqual(2, mgr2.getNodes().Count, "节点数应往返一致");
                Assert.AreEqual(1, mgr2.getConnectors().Count, "连接数应往返一致");

                var add2 = mgr2.getNodes().First(n => ((NodeBase)n).NodePath == addPath);
                Assert.AreEqual(7, Convert.ToInt32(add2.findPin("a").data), "引脚 a 默认值应往返");
                Assert.AreEqual(5, Convert.ToInt32(add2.findPin("b").data), "引脚 b 默认值应往返");
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void SaveLoad_RoundTrips_ConstantNodeValueAndType()
        {
            var svc = NewService(out var mgr);

            var intConstPath = NodeFactory.GetSystemNodePaths().First(p => p.EndsWith("Int"));
            var node = NodeFactory.CreateNode(intConstPath);
            var prop = node.GetType().GetProperty("Value");
            prop.SetValue(node, 42);
            mgr.addNode(node);

            var temp = Path.GetTempFileName();
            try
            {
                svc.SaveToFile(temp);

                var svc2 = NewService(out var mgr2);
                svc2.LoadFromFile(temp);

                var loaded = mgr2.getNodes().Single();
                Assert.AreEqual(42, loaded.GetType().GetProperty("Value").GetValue(loaded),
                    "常量节点的 Value 属性应往返一致");
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void ConvertedPinDefault_PreservesTypeNotJustString()
        {
            var svc = NewService(out var mgr);

            var addPath = NodeFactory.GetFunctionNodePaths().First(p => p.EndsWith("add"));
            var add = NodeFactory.CreateNode(addPath);
            add.findPin("a").data = 11;
            add.findPin("b").data = 4;
            mgr.addNode(add);

            var temp = Path.GetTempFileName();
            try
            {
                svc.SaveToFile(temp);

                var svc2 = NewService(out var mgr2);
                svc2.LoadFromFile(temp);

                var loaded = (NodeBase)mgr2.getNodes().Single();
                var pinA = loaded.findPin("a");
                Assert.IsInstanceOf<int>(pinA.data, "还原后的引脚值应为 int 而非字符串");

                // 进一步：执行后应可正确计算，证明类型正确
                loaded.excute(mgr2);
                Assert.AreEqual(15, loaded.findPin("result").data);
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void SaveLoad_RoundTrips_VarNodes()
        {
            var svc = NewService(out var mgr);

            mgr.SetDataObject("score", 99, typeof(int));

            var getNode = NodeFactory.CreateVarNode("score", typeof(int), isSet: false);
            var printPath = NodeFactory.GetSystemNodePaths().First(p => p.EndsWith("Print"));
            var print = NodeFactory.CreateNode(printPath);
            mgr.addNode(getNode);
            mgr.addNode(print);
            mgr.addConnector(getNode.findPin("score"), print.findPin("Value"));

            var setNode = NodeFactory.CreateVarNode("flag", typeof(bool), isSet: true);
            mgr.addNode(setNode);

            var temp = Path.GetTempFileName();
            try
            {
                svc.SaveToFile(temp);

                var svc2 = NewService(out var mgr2);
                svc2.LoadFromFile(temp);

                Assert.AreEqual(3, mgr2.getNodes().Count);
                Assert.AreEqual(1, mgr2.getConnectors().Count);

                Assert.AreEqual(99, Convert.ToInt32(mgr2.GetDataObject("score")), "全局变量 score 取值应往返");
                Assert.AreEqual(typeof(int), mgr2.GetDataObjectType("score"));

                var loadedGet = mgr2.getNodes().OfType<GetObjectNode>().Single();
                Assert.AreEqual("score", loadedGet.VariableName);
                Assert.AreEqual(typeof(int), loadedGet.VariableType);
                Assert.IsTrue(loadedGet.IsAutoRun);

                var loadedSet = mgr2.getNodes().OfType<SetObjectNode>().Single();
                Assert.AreEqual("flag", loadedSet.VariableName);
                Assert.AreEqual(typeof(bool), loadedSet.VariableType);
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void SaveLoad_RoundTrips_GlobalDataObjectValues()
        {
            var svc = NewService(out var mgr);

            mgr.SetDataObject("name", "alice", typeof(string));
            mgr.SetDataObject("ratio", 0.75, typeof(double));
            mgr.SetDataObject("enabled", true, typeof(bool));

            var temp = Path.GetTempFileName();
            try
            {
                svc.SaveToFile(temp);

                var svc2 = NewService(out var mgr2);
                svc2.LoadFromFile(temp);

                Assert.AreEqual("alice", mgr2.GetDataObject("name"));
                Assert.AreEqual(0.75, Convert.ToDouble(mgr2.GetDataObject("ratio")), 1e-9);
                Assert.AreEqual(true, mgr2.GetDataObject("enabled"));
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void ComputeContentFingerprint_IsStableForSameGraph()
        {
            var svc = NewService(out var mgr);

            var addPath = NodeFactory.GetFunctionNodePaths().First(p => p.EndsWith("add"));
            var add = NodeFactory.CreateNode(addPath);
            mgr.addNode(add);
            mgr.SetDataObject("x", 1, typeof(int));

            var fp1 = svc.ComputeContentFingerprint();
            var fp2 = svc.ComputeContentFingerprint();

            Assert.AreEqual(fp1, fp2, "相同图内容指纹应一致");
        }
    }
}
