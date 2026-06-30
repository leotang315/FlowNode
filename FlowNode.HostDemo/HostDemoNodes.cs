using FlowNode.node;
using FlowNode.node.Attribute;

namespace FlowNode.HostDemo
{
    /// <summary>
    /// 宿主侧自定义节点示例：注册后可在节点搜索里看到 Demo/formatGrade。
    /// </summary>
    [Node("Demo")]
    public static class HostDemoNodes
    {
        [Function("formatGrade", true)]
        public static void FormatGrade(int score, int threshold, out string label)
        {
            label = score >= threshold ? "PASS" : "FAIL";
        }
    }
}
