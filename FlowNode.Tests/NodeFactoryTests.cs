using System;
using System.Linq;
using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class NodeFactoryTests
    {
        [Test]
        public void SystemNodes_AreDiscoveredViaReflection()
        {
            var paths = NodeFactory.GetSystemNodePaths();
            Assert.IsNotEmpty(paths, "应通过反射发现至少一个系统节点（如 TestNode）");
        }

        [Test]
        public void FunctionNodes_AreDiscoveredViaReflection()
        {
            var paths = NodeFactory.GetFunctionNodePaths();
            Assert.IsNotEmpty(paths, "应通过反射发现至少一个函数节点（如 MathOperator.add）");
        }

        [Test]
        public void CreateNode_FromSystemPath_ReturnsInitializedNode()
        {
            var path = NodeFactory.GetSystemNodePaths().First();

            var node = NodeFactory.CreateNode(path);

            Assert.IsNotNull(node);
            Assert.AreEqual(path, node.NodePath);
            Assert.IsNotEmpty(node.Pins, "节点 init 后应已分配引脚");
        }

        [Test]
        public void CreateNode_InvalidPath_Throws()
        {
            Assert.Throws<ArgumentException>(() => NodeFactory.CreateNode("/no/such/node"));
        }
    }
}
