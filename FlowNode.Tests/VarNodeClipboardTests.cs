using FlowNode.app.command;
using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class VarNodeClipboardTests
    {
        [Test]
        public void TryGetVarNodeInfo_GetObjectNode_ReturnsMetadata()
        {
            var node = NodeFactory.CreateVarNode("score", typeof(int), isSet: false);

            Assert.IsTrue(NodeFactory.TryGetVarNodeInfo(node, out var name, out var type, out var isSet));
            Assert.AreEqual("score", name);
            Assert.AreEqual(typeof(int), type);
            Assert.IsFalse(isSet);
        }

        [Test]
        public void TryGetVarNodeInfo_SetObjectNode_ReturnsMetadata()
        {
            var node = NodeFactory.CreateVarNode("flag", typeof(bool), isSet: true);

            Assert.IsTrue(NodeFactory.TryGetVarNodeInfo(node, out var name, out var type, out var isSet));
            Assert.AreEqual("flag", name);
            Assert.AreEqual(typeof(bool), type);
            Assert.IsTrue(isSet);
        }

        [Test]
        public void CreateVarNodeFromInfo_RecreatesMatchingNode()
        {
            var original = NodeFactory.CreateVarNode("name", typeof(string), isSet: false);
            Assert.IsTrue(NodeFactory.TryGetVarNodeInfo(original, out var name, out var type, out var isSet));

            var copy = NodeFactory.CreateVarNodeFromInfo(name, type.AssemblyQualifiedName, isSet);

            Assert.IsInstanceOf<GetObjectNode>(copy);
            Assert.AreEqual("Get name", copy.Name);
            Assert.AreEqual("name", copy.findPin("name").Name);
        }
    }
}
