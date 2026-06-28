using System.Linq;
using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class FunctionNodeSubtitleTests
    {
        [Test]
        public void GetDisplaySubtitle_ShowsMethodNameAndInputs()
        {
            var addPath = NodeFactory.GetFunctionNodePaths().First(p => p.EndsWith("add"));
            var node = NodeFactory.CreateNode(addPath);
            node.findPin("a").data = 3;
            node.findPin("b").data = 5;

            var subtitle = node.GetDisplaySubtitle();

            Assert.IsTrue(subtitle.StartsWith("add"));
            Assert.IsTrue(subtitle.Contains("a=3"));
            Assert.IsTrue(subtitle.Contains("b=5"));
        }

        [Test]
        public void GetDisplaySubtitle_CompareNode_ShowsOperatorName()
        {
            var path = NodeFactory.GetFunctionNodePaths().First(p => p.EndsWith("greater"));
            var node = NodeFactory.CreateNode(path);

            Assert.IsTrue(node.GetDisplaySubtitle().StartsWith("Greater"));
        }
    }
}
