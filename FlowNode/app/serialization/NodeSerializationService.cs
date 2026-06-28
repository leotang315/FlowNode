using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using FlowNode.node;
using FlowNode.app.view;
namespace FlowNode.app.serialization
{
    public class NodeSerializationService
    {
        private readonly NodeManager nodeManager;
        private readonly Dictionary<INode, NodeView> nodeViews;

        public NodeSerializationService(NodeManager nodeManager, Dictionary<INode, NodeView> nodeViews)
        {
            this.nodeManager = nodeManager;
            this.nodeViews = nodeViews;
        }

        /// <summary>
        /// 将当前节点图保存到文件
        /// </summary>
        public void SaveToFile(string filePath)
        {
            var graphData = SerializeGraph();
            using (var stream = File.Create(filePath))
            {
                var serializer = new XmlSerializer(typeof(NodeGraphData));
                serializer.Serialize(stream, graphData);
            }
        }

        /// <summary>
        /// 从文件加载节点图
        /// </summary>
        public void LoadFromFile(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                var serializer = new XmlSerializer(typeof(NodeGraphData));
                var graphData = (NodeGraphData)serializer.Deserialize(stream);
                DeserializeGraph(graphData);
            }
        }

        /// <summary>
        /// 计算当前图内容的 SHA256 指纹，用于判断是否与上次保存/加载状态一致。
        /// 使用稳定节点 Id（按列表顺序），与磁盘文件中的 GUID Id 无关。
        /// </summary>
        public string ComputeContentFingerprint()
        {
            var graphData = SerializeGraph(useStableNodeIds: true);
            graphData.DataObjects = graphData.DataObjects
                .OrderBy(d => d.Key, StringComparer.Ordinal)
                .ToList();

            using (var stream = new MemoryStream())
            {
                var serializer = new XmlSerializer(typeof(NodeGraphData));
                serializer.Serialize(stream, graphData);
                using (var sha = SHA256.Create())
                {
                    return Convert.ToBase64String(sha.ComputeHash(stream.ToArray()));
                }
            }
        }

        private NodeGraphData SerializeGraph(bool useStableNodeIds = false)
        {
            var graphData = new NodeGraphData();
            var nodeIdMap = new Dictionary<INode, string>();
            var nodeIndex = 0;

            // 序列化节点
            foreach (var node in nodeManager.getNodes())
            {
                var nodeId = useStableNodeIds
                    ? nodeIndex.ToString(CultureInfo.InvariantCulture)
                    : Guid.NewGuid().ToString();
                nodeIndex++;
                nodeIdMap[node] = nodeId;

                var nodeData = new NodeData
                {
                    Id = nodeId,
                    Type = node.GetType().FullName,
                    Name = node.Name,
                    IsAutoRun = node.IsAutoRun,
                    NodePath = node.NodePath
                };

                // 序列化节点属性
                nodeData.Properties = NodeSnapshotHelper.CaptureProperties((NodeBase)node);

                // 序列化引脚
                nodeData.Pins = NodeSnapshotHelper.CapturePins((NodeBase)node);

                if (NodeFactory.TryGetVarNodeInfo((NodeBase)node, out var varName, out var varType, out var isSet))
                {
                    nodeData.VarName = varName;
                    nodeData.VarTypeName = varType.AssemblyQualifiedName;
                    nodeData.VarIsSet = isSet;
                }

                graphData.Nodes.Add(nodeData);

                // 序列化视图数据
                if (nodeViews.TryGetValue(node, out var nodeView))
                {
                    graphData.ViewData.Add(new NodeViewData
                    {
                        NodeId = nodeId,
                        X = nodeView.Bounds.X,
                        Y = nodeView.Bounds.Y,
                        Width = nodeView.Bounds.Width,
                        Height = nodeView.Bounds.Height
                    });
                }
            }

            // 序列化连接器
            foreach (var connector in nodeManager.getConnectors())
            {
                graphData.Connectors.Add(new ConnectorData
                {
                    SourceNodeId = nodeIdMap[connector.src.host],
                    SourcePinName = connector.src.Name,
                    TargetNodeId = nodeIdMap[connector.dst.host],
                    TargetPinName = connector.dst.Name
                });
            }

            foreach (var key in nodeManager.GetAllDataObjectKeys().OrderBy(k => k, StringComparer.Ordinal))
            {
                var value = nodeManager.GetDataObject(key);
                var type = nodeManager.GetDataObjectType(key);
                if (type == null)
                    continue;

                var data = new DataObjectData
                {
                    Key = key,
                    TypeName = type.AssemblyQualifiedName
                };
                if (value != null)
                {
                    data.Value = Convert.ToString(value, CultureInfo.InvariantCulture);
                    data.ValueTypeName = value.GetType().FullName;
                }

                graphData.DataObjects.Add(data);
            }

            return graphData;
        }

        private void DeserializeGraph(NodeGraphData graphData)
        {
            // 清除现有数据
            nodeManager.clear();
            nodeViews.Clear();

            if (graphData.DataObjects != null)
            {
                foreach (var data in graphData.DataObjects)
                {
                    if (string.IsNullOrEmpty(data.Key) || string.IsNullOrEmpty(data.TypeName))
                        continue;

                    var type = Type.GetType(data.TypeName);
                    if (type == null)
                        continue;

                    object value = null;
                    if (data.Value != null)
                    {
                        value = PinValueConverter.ConvertStringToValue(
                            data.Value, data.ValueTypeName, type);
                    }

                    nodeManager.SetDataObject(data.Key, value, type);
                }
            }

            var nodeMap = new Dictionary<string, INode>();

            // 恢复节点
            foreach (var nodeData in graphData.Nodes)
            {
                NodeBase node;
                try
                {
                    node = NodeRestoreHelper.CreateFromSerializedData(
                        nodeData.NodePath,
                        nodeData.VarName,
                        nodeData.VarTypeName,
                        nodeData.VarIsSet,
                        nodeData.Name,
                        nodeData.IsAutoRun,
                        nodeData.Properties,
                        nodeData.Pins);

                    nodeManager.addNode(node);
                    nodeMap[nodeData.Id] = node;

                    // 恢复视图
                    var viewData = graphData.ViewData.FirstOrDefault(v => v.NodeId == nodeData.Id);
                    if (viewData != null)
                    {
                        var bounds = new Rectangle(viewData.X, viewData.Y, viewData.Width, viewData.Height);
                        var nodeView = NodeViewFactory.CreateNodeView((NodeBase)node, new Point(bounds.X, bounds.Y));
                        nodeView.Bounds = bounds;
                        nodeViews.Add(node, nodeView);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error creating node: {ex.Message}");
                    continue;
                }
            }

            // 恢复连接器
            foreach (var connectorData in graphData.Connectors)
            {
                try
                {
                    var sourceNode = nodeMap[connectorData.SourceNodeId];
                    var targetNode = nodeMap[connectorData.TargetNodeId];
                    var sourcePin = sourceNode.findPin(connectorData.SourcePinName);
                    var targetPin = targetNode.findPin(connectorData.TargetPinName);

                    if (sourcePin != null && targetPin != null)
                    {
                        nodeManager.addConnector(sourcePin, targetPin);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error restoring connector: {ex.Message}");
                    continue;
                }
            }
        }
    }
} 
