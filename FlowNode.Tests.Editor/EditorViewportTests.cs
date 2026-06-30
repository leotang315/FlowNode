using System.Drawing;
using FlowNode.app.view;
using NUnit.Framework;

namespace FlowNode.Tests.Editor
{
    [TestFixture]
    public class EditorViewportTests
    {
        [Test]
        public void WorldToClientRect_AppliesZoomAndPan()
        {
            var world = new RectangleF(10, 20, 100, 50);
            var client = EditorViewport.WorldToClientRect(world, zoom: 2f, panOffset: new Point(5, 10), controlSize: new Size(800, 600));

            Assert.AreEqual(25, client.Left);
            Assert.AreEqual(50, client.Top);
            Assert.AreEqual(225, client.Right);
            Assert.AreEqual(150, client.Bottom);
        }

        [Test]
        public void GetConnectorWorldBounds_IncludesBezierTangentExtent()
        {
            var start = new Point(0, 0);
            var end = new Point(200, 0);
            var bounds = EditorViewport.GetConnectorWorldBounds(start, end);

            Assert.GreaterOrEqual(bounds.Right, start.X + 100);
            Assert.LessOrEqual(bounds.Left, end.X - 100);
        }

        [Test]
        public void UnionAll_MergesMultipleRects()
        {
            var a = new RectangleF(0, 0, 10, 10);
            var b = new RectangleF(20, 5, 10, 10);
            var union = EditorViewport.UnionAll(new[] { a, b });

            Assert.AreEqual(0, union.Left);
            Assert.AreEqual(0, union.Top);
            Assert.AreEqual(30, union.Right);
            Assert.AreEqual(15, union.Bottom);
        }
    }
}
