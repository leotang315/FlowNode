using System.Collections.Generic;
using FlowNode.node;
using NUnit.Framework;

namespace FlowNode.Tests
{
    [TestFixture]
    public class SwitchNodeTests
    {
        private static List<string> CollectPrintLogs(NodeManager mgr)
        {
            var logs = new List<string>();
            mgr.Log += s =>
            {
                if (s != null && s.StartsWith("[Print]"))
                    logs.Add(s);
            };
            return logs;
        }

        [Test]
        public void Switch_RoutesMatchingCase()
        {
            var mgr = new NodeManager();
            var logs = CollectPrintLogs(mgr);

            var sw = new SwitchNode { CaseCount = 3 };
            sw.init();
            sw.findPin("Index").data = 1;

            var printCase = new PrintNode();
            printCase.init();
            printCase.findPin("Value").data = "case1";

            var printDefault = new PrintNode();
            printDefault.init();
            printDefault.findPin("Value").data = "default";

            mgr.addNode(sw);
            mgr.addNode(printCase);
            mgr.addNode(printDefault);
            mgr.addConnector(sw.findPin("Case1"), printCase.findPin("Input"));
            mgr.addConnector(sw.findPin("Default"), printDefault.findPin("Input"));

            mgr.run();

            Assert.AreEqual(1, logs.Count);
            StringAssert.Contains("case1", logs[0]);
        }

        [Test]
        public void Switch_OutOfRange_GoesToDefault()
        {
            var mgr = new NodeManager();
            var logs = CollectPrintLogs(mgr);

            var sw = new SwitchNode { CaseCount = 2 };
            sw.init();
            sw.findPin("Index").data = 5;

            var printDefault = new PrintNode();
            printDefault.init();
            printDefault.findPin("Value").data = "fallback";

            mgr.addNode(sw);
            mgr.addNode(printDefault);
            mgr.addConnector(sw.findPin("Default"), printDefault.findPin("Input"));

            mgr.run();

            Assert.AreEqual(1, logs.Count);
            StringAssert.Contains("fallback", logs[0]);
        }

        [Test]
        public void GetDisplaySubtitle_ShowsTargetCase()
        {
            var sw = new SwitchNode { CaseCount = 3 };
            sw.init();
            sw.findPin("Index").data = 2;
            Assert.AreEqual("→Case2", sw.GetDisplaySubtitle());

            sw.findPin("Index").data = 9;
            Assert.AreEqual("→Default", sw.GetDisplaySubtitle());
        }
    }
}
