using System;
using System.Collections.Generic;
using FlowNode.node;

namespace FlowNode.app.serialization
{
    /// <summary>
    /// 节点图的序列化数据
    /// </summary>
    [Serializable]
    public class NodeGraphData
    {
        public List<NodeData> Nodes { get; set; } = new List<NodeData>();
        public List<ConnectorData> Connectors { get; set; } = new List<ConnectorData>();
        public List<NodeViewData> ViewData { get; set; } = new List<NodeViewData>();
    }

    /// <summary>
    /// 单个节点的序列化数据
    /// </summary>
    [Serializable]
    public class NodeData
    {
        public string Id { get; set; }  // 节点的唯一标识
        public string Type { get; set; } // 节点的类型名
        public string Name { get; set; }
        public bool IsAutoRun { get; set; }
        public string NodePath { get; set; }
        public List<PropertyData> Properties { get; set; } = new List<PropertyData>();
        public List<PinData> Pins { get; set; } = new List<PinData>();
    }

    /// <summary>
    /// 属性的序列化数据
    /// </summary>
    [Serializable]
    public class PropertyData
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string TypeName { get; set; }
    }

    /// <summary>
    /// 引脚的序列化数据
    /// </summary>
    [Serializable]
    public class PinData
    {
        public string Name { get; set; }
        public PinDirection Direction { get; set; }
        public PinType PinType { get; set; }
        public string DataType { get; set; }
        public string DefaultValue { get; set; }
        public string ValueTypeName { get; set; }
    }

    /// <summary>
    /// 连接器的序列化数据
    /// </summary>
    [Serializable]
    public class ConnectorData
    {
        public string SourceNodeId { get; set; }
        public string SourcePinName { get; set; }
        public string TargetNodeId { get; set; }
        public string TargetPinName { get; set; }
    }

    /// <summary>
    /// 节点视图的序列化数据
    /// </summary>
    [Serializable]
    public class NodeViewData
    {
        public string NodeId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
} 