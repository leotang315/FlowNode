using System.Collections.Generic;
using System.Drawing;
using FlowNode.app.view;
using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class NodeEditorLayoutTests
    {
        private static NodeView CreateView(int x, int y, int width = 100)
        {
            var node = new PrintNode();
            node.init();
            var view = new DefaultNodeView(node, new Point(x, y));
            view.Bounds = new Rectangle(x, y, width, view.Bounds.Height);
            return view;
        }

        [Test]
        public void AlignLeft_MovesViewsToLeftmostEdge()
        {
            var a = CreateView(10, 0);
            var b = CreateView(50, 20, 80);
            var moves = NodeEditorLayout.Align(new List<NodeView> { a, b }, NodeAlignEdge.Left);

            Assert.AreEqual(1, moves.Count);
            Assert.AreEqual(new Point(10, 20), moves[b]);
        }

        [Test]
        public void AlignRight_MovesViewsToRightmostEdge()
        {
            var a = CreateView(0, 0, 100);
            var b = CreateView(120, 10, 60);
            var moves = NodeEditorLayout.Align(new List<NodeView> { a, b }, NodeAlignEdge.Right);

            Assert.AreEqual(1, moves.Count);
            Assert.AreEqual(new Point(80, 0), moves[a]);
        }

        [Test]
        public void DistributeHorizontally_SpacesViewsEvenly()
        {
            var a = CreateView(0, 0, 50);
            var b = CreateView(100, 0, 50);
            var c = CreateView(250, 0, 50);
            var moves = NodeEditorLayout.DistributeHorizontally(new List<NodeView> { a, b, c });

            Assert.AreEqual(1, moves.Count);
            Assert.AreEqual(new Point(125, 0), moves[b]);
        }

        [Test]
        public void DistributeVertically_SpacesViewsEvenly()
        {
            var a = CreateView(0, 0);
            var b = CreateView(0, 100);
            var c = CreateView(0, 400);
            var moves = NodeEditorLayout.DistributeVertically(new List<NodeView> { a, b, c });

            Assert.AreEqual(1, moves.Count);
            Assert.AreEqual(new Point(0, 200), moves[b]);
        }

        [Test]
        public void Align_WithSingleView_ReturnsEmpty()
        {
            var a = CreateView(0, 0);
            var moves = NodeEditorLayout.Align(new List<NodeView> { a }, NodeAlignEdge.Left);
            Assert.AreEqual(0, moves.Count);
        }
    }
}
