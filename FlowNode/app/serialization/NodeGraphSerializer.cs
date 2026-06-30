using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Serialization;
using FlowNode.node;

namespace FlowNode.app.serialization
{
    /// <summary>
    /// 节点图序列化（逻辑层）：节点、连线、引脚、全局变量。不含编辑器布局。
    /// </summary>
    public class NodeGraphSerializer
    {
        private readonly NodeManager nodeManager;

        public NodeGraphSerializer(NodeManager nodeManager)
        {
            this.nodeManager = nodeManager ?? throw new ArgumentNullException(nameof(nodeManager));
        }

        public NodeManager NodeManager => nodeManager;

        public void SaveToFile(string filePath, Func<INode, NodeViewData> getViewData = null)
        {
            var graphData = SerializeGraph(getViewData: getViewData);
            using (var stream = File.Create(filePath))
            {
                var serializer = new XmlSerializer(typeof(NodeGraphData));
                serializer.Serialize(stream, graphData);
            }
        }

        public void LoadFromFile(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                var serializer = new XmlSerializer(typeof(NodeGraphData));
                var graphData = (NodeGraphData)serializer.Deserialize(stream);
                DeserializeGraph(graphData);
            }
        }

        public string ComputeContentFingerprint(Func<INode, NodeViewData> getViewData = null)
        {
            var graphData = SerializeGraph(useStableNodeIds: true, getViewData: getViewData);
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

        public NodeGraphData SerializeGraph(
            bool useStableNodeIds = false,
            Func<INode, NodeViewData> getViewData = null)
        {
            var graphData = new NodeGraphData();
            var nodeIdMap = new Dictionary<INode, string>();
            var nodeIndex = 0;

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

                nodeData.Properties = NodeSnapshotHelper.CaptureProperties((NodeBase)node);
                nodeData.Pins = NodeSnapshotHelper.CapturePins((NodeBase)node);

                if (NodeFactory.TryGetVarNodeInfo((NodeBase)node, out var varName, out var varType, out var isSet))
                {
                    nodeData.VarName = varName;
                    nodeData.VarTypeName = varType.AssemblyQualifiedName;
                    nodeData.VarIsSet = isSet;
                }

                graphData.Nodes.Add(nodeData);

                if (getViewData != null)
                {
                    var viewData = getViewData(node);
                    if (viewData != null)
                    {
                        viewData.NodeId = nodeId;
                        graphData.ViewData.Add(viewData);
                    }
                }
            }

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

        public Dictionary<string, INode> DeserializeGraph(NodeGraphData graphData)
        {
            nodeManager.clear();

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

            foreach (var nodeData in graphData.Nodes)
            {
                try
                {
                    var node = NodeRestoreHelper.CreateFromSerializedData(
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
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error creating node: {ex.Message}");
                }
            }

            if (graphData.Connectors == null)
                return nodeMap;

            foreach (var connectorData in graphData.Connectors)
            {
                try
                {
                    if (!nodeMap.TryGetValue(connectorData.SourceNodeId, out var sourceNode))
                        continue;
                    if (!nodeMap.TryGetValue(connectorData.TargetNodeId, out var targetNode))
                        continue;

                    var sourcePin = sourceNode.findPin(connectorData.SourcePinName);
                    var targetPin = targetNode.findPin(connectorData.TargetPinName);

                    if (sourcePin != null && targetPin != null)
                        nodeManager.addConnector(sourcePin, targetPin);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error restoring connector: {ex.Message}");
                }
            }

            SyncGetObjectOutputPins(nodeManager);

            return nodeMap;
        }

        private static void SyncGetObjectOutputPins(NodeManager manager)
        {
            manager.SyncGetObjectOutputPins();
        }
    }
}
