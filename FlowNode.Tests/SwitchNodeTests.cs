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
            Assert.AreEqual("3路 →Case2", sw.GetDisplaySubtitle());

            sw.findPin("Index").data = 9;
            Assert.AreEqual("3路 →Default", sw.GetDisplaySubtitle());
        }

        [Test]
        public void SyncCasePins_MatchesCaseCount()
        {
            var mgr = new NodeManager();
            var sw = new SwitchNode { CaseCount = 2 };
            sw.init();
            mgr.addNode(sw);

            Assert.IsNotNull(sw.findPin("Case0"));
            Assert.IsNotNull(sw.findPin("Case1"));
            Assert.IsNull(sw.findPin("Case2"));

            sw.CaseCount = 4;
            sw.SyncCasePins(mgr);

            Assert.IsNotNull(sw.findPin("Case3"));
            Assert.IsNull(sw.findPin("Case4"));
        }

        [Test]
        public void SyncCasePins_ClampsToMaxCases()
        {
            var mgr = new NodeManager();
            var sw = new SwitchNode { CaseCount = 2 };
            sw.init();
            mgr.addNode(sw);

            sw.CaseCount = 50;
            sw.SyncCasePins(mgr);

            Assert.AreEqual(SwitchNode.MaxCases, sw.GetEffectiveCaseCount());
            Assert.IsNotNull(sw.findPin("Case31"));
            Assert.IsNull(sw.findPin("Case32"));
        }

        [Test]
        public void SyncCasePins_RemovingCaseRemovesConnectors()
        {
            var mgr = new NodeManager();
            var sw = new SwitchNode { CaseCount = 3 };
            sw.init();
            var print = new PrintNode();
            print.init();
            mgr.addNode(sw);
            mgr.addNode(print);
            mgr.addConnector(sw.findPin("Case2"), print.findPin("Input"));

            sw.CaseCount = 2;
            sw.SyncCasePins(mgr);

            Assert.IsNull(sw.findPin("Case2"));
            Assert.AreEqual(0, mgr.getConnectors().Count);
        }
    }
}
