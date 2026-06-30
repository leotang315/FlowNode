using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using FlowNode.app.view;
using FlowNode.node;

namespace FlowNode.app.serialization
{
    /// <summary>
    /// 编辑器序列化：在 <see cref="NodeGraphSerializer"/> 之上恢复/保存节点视图布局。
    /// </summary>
    public class NodeSerializationService
    {
        private readonly NodeGraphSerializer graphSerializer;
        private readonly Dictionary<INode, NodeView> nodeViews;

        public NodeSerializationService(NodeManager nodeManager, Dictionary<INode, NodeView> nodeViews)
        {
            this.graphSerializer = new NodeGraphSerializer(nodeManager);
            this.nodeViews = nodeViews;
        }

        public void SaveToFile(string filePath)
        {
            graphSerializer.SaveToFile(filePath, CaptureViewData);
        }

        public void LoadFromFile(string filePath)
        {
            using (var stream = System.IO.File.OpenRead(filePath))
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(NodeGraphData));
                var graphData = (NodeGraphData)serializer.Deserialize(stream);
                LoadGraph(graphData);
            }
        }

        public string ComputeContentFingerprint()
        {
            return graphSerializer.ComputeContentFingerprint(CaptureViewData);
        }

        private NodeViewData CaptureViewData(INode node)
        {
            if (!nodeViews.TryGetValue(node, out var nodeView))
                return null;

            return new NodeViewData
            {
                X = nodeView.Bounds.X,
                Y = nodeView.Bounds.Y,
                Width = nodeView.Bounds.Width,
                Height = nodeView.Bounds.Height
            };
        }

        private void LoadGraph(NodeGraphData graphData)
        {
            nodeViews.Clear();
            var nodeMap = graphSerializer.DeserializeGraph(graphData);

            var positionedNodeIds = new HashSet<string>(StringComparer.Ordinal);

            if (graphData.ViewData != null)
            {
                foreach (var viewData in graphData.ViewData)
                {
                    if (string.IsNullOrEmpty(viewData.NodeId))
                        continue;
                    if (!nodeMap.TryGetValue(viewData.NodeId, out var node))
                        continue;

                    var bounds = new Rectangle(viewData.X, viewData.Y, viewData.Width, viewData.Height);
                    var nodeView = NodeViewFactory.CreateNodeView((NodeBase)node, new Point(bounds.X, bounds.Y));
                    nodeView.Bounds = bounds;
                    nodeViews.Add(node, nodeView);
                    positionedNodeIds.Add(viewData.NodeId);
                }
            }

            // CLI / SampleGenerator 生成的图常无 ViewData；为缺失视图的节点自动排布，避免画布空白。
            var fallbackIndex = 0;
            foreach (var pair in nodeMap.OrderBy(p => p.Value.Name, StringComparer.Ordinal))
            {
                if (positionedNodeIds.Contains(pair.Key))
                    continue;

                const int columnWidth = 240;
                const int rowHeight = 140;
                var location = new Point(
                    40 + (fallbackIndex % 3) * columnWidth,
                    40 + (fallbackIndex / 3) * rowHeight);
                fallbackIndex++;

                var nodeView = NodeViewFactory.CreateNodeView((NodeBase)pair.Value, location);
                nodeViews.Add(pair.Value, nodeView);
            }
        }
    }
}
