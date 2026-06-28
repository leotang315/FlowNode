using FlowNode.node;

namespace FlowNode.app.serialization
{
    /// <summary>
    /// 从序列化/剪贴板元数据重建节点，供文件加载与粘贴共用。
    /// </summary>
    public static class NodeRestoreHelper
    {
        public static NodeBase CreateFromSerializedData(
            string nodePath,
            string varName,
            string varTypeName,
            bool varIsSet,
            string name,
            bool isAutoRun,
            System.Collections.Generic.List<PropertyData> properties,
            System.Collections.Generic.List<PinData> pins)
        {
            NodeBase node;
            if (!string.IsNullOrEmpty(varName))
            {
                node = NodeFactory.CreateVarNodeFromInfo(varName, varTypeName, varIsSet);
            }
            else if (!string.IsNullOrEmpty(nodePath))
            {
                node = NodeFactory.CreateNode(nodePath);
            }
            else
            {
                throw new System.InvalidOperationException("节点缺少 NodePath 或变量元数据，无法重建");
            }

            node.Name = name;
            node.IsAutoRun = isAutoRun;
            NodeSnapshotHelper.Apply(node, properties, pins);
            return node;
        }
    }
}
