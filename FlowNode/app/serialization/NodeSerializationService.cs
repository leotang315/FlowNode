using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
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

        private NodeGraphData SerializeGraph()
        {
            var graphData = new NodeGraphData();
            var nodeIdMap = new Dictionary<INode, string>();

            // 序列化节点
            foreach (var node in nodeManager.getNodes())
            {
                var nodeId = Guid.NewGuid().ToString();
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
                var properties = node.GetType().GetProperties();
                foreach (var property in properties)
                {
                    if (property.Name == "Name" || 
                    property.Name == "Pins"|| 
                    property.Name == "IsAutoRun"||
                    property.Name == "NodePath")
                        continue;

                    try
                    {
                        var value = property.GetValue(node);
                        if (value != null)
                        {
                            nodeData.Properties.Add(new PropertyData
                            {
                                Key = property.Name,
                                Value = Convert.ToString(value),
                                TypeName = value.GetType().FullName
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error serializing property {property.Name}: {ex.Message}");
                    }
                }

                // 序列化引脚
                foreach (var pin in node.Pins)
                {
                    nodeData.Pins.Add(new PinData
                    {
                        Name = pin.Name,
                        Direction = pin.direction,
                        PinType = pin.pinType,
                        DataType = pin.dataType?.FullName,
                        DefaultValue = pin.data?.ToString(),
                        ValueTypeName = pin.data?.GetType().FullName
                    });
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

            return graphData;
        }

        private void DeserializeGraph(NodeGraphData graphData)
        {
            // 清除现有数据
            nodeManager.clear();
            nodeViews.Clear();

            var nodeMap = new Dictionary<string, INode>();

            // 恢复节点
            foreach (var nodeData in graphData.Nodes)
            {
                NodeBase node;
                try
                {
                    // 从节点路径创建节点
                    node = NodeFactory.CreateNode(nodeData.NodePath);


                    node.Name = nodeData.Name;

                    // 恢复节点属性
                    foreach (var property in nodeData.Properties)
                    {
                        if (property.Key == "NodePath") continue;

                        try
                        {
                            var propertyInfo = node.GetType().GetProperty(property.Key);
                            if (propertyInfo != null && propertyInfo.CanWrite)
                            {
                                if (property.TypeName != null)
                                {
                                    var type = Type.GetType(property.TypeName);
                                    if (type != null)
                                    {
                                        var convertedValue = Convert.ChangeType(property.Value, type);
                                        propertyInfo.SetValue(node, convertedValue);
                                    }
                                }
                                else
                                {
                                    propertyInfo.SetValue(node, property.Value);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error deserializing property {property.Key}: {ex.Message}");
                        }
                    }

                    //// 恢复引脚数据
                    //foreach (var pinData in nodeData.Pins)
                    //{
                    //    var pin = node.findPin(pinData.Name);
                    //    if (pin != null)
                    //    {
                    //        pin.data = pinData.DefaultValue;
                    //    }
                    //}

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
