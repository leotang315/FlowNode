using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class GetObjectNodeTests
    {
        [Test]
        public void GetDisplaySubtitle_ShowsValueAfterRefreshFromManager()
        {
            var mgr = new NodeManager();
            mgr.SetDataObject("threshold", 60, typeof(int));

            var get = new GetObjectNode("threshold", typeof(int));
            get.init();
            get.RefreshOutputFrom(mgr);

            Assert.AreEqual("60", get.GetDisplaySubtitle());
        }

        [Test]
        public void GetDisplaySubtitle_ShowsUnsetWhenVariableMissing()
        {
            var get = new GetObjectNode("threshold", typeof(int));
            get.init();
            get.RefreshOutputFrom(new NodeManager());

            Assert.AreEqual("未设置", get.GetDisplaySubtitle());
        }
    }
}
