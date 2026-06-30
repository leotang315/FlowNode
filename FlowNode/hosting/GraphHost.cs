using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FlowNode.app.serialization;
using FlowNode.node;

namespace FlowNode.hosting
{
    /// <summary>
    /// 嵌入 FlowNode 的宿主入口：创建 NodeManager、注册扩展节点、加载并执行图。
    /// </summary>
    public interface IGraphHost
    {
        NodeManager NodeManager { get; }

        /// <summary>扫描程序集中的 [SystemNode] / [Function] 并注册（可重复调用，已注册路径跳过）。</summary>
        void RegisterAssembly(Assembly assembly);

        GraphRunResult RunFile(string filePath, GraphRunOptions options = null);
    }

    public sealed class GraphHost : IGraphHost
    {
        public GraphHost()
        {
            NodeManager = new NodeManager();
        }

        public NodeManager NodeManager { get; }

        public void RegisterAssembly(Assembly assembly)
        {
            NodeFactory.RegisterAssembly(assembly);
        }

        public GraphRunResult RunFile(string filePath, GraphRunOptions options = null)
        {
            return GraphRunner.RunFileInto(NodeManager, filePath, options);
        }
    }
}
