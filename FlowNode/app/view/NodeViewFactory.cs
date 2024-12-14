using FlowNode.node;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace FlowNode.app.view
{
    public static class NodeViewFactory
    {
        private static Dictionary<Type, Type> nodeViewMappings = new Dictionary<Type, Type>();

        // 注册节点类型和对应的NodeView类型
        public static void RegisterNodeView<TNode, TNodeView>() 
            where TNode : NodeBase 
            where TNodeView : NodeView
        {
            nodeViewMappings[typeof(TNode)] = typeof(TNodeView);
        }

        // 创建NodeView实例
        public static NodeView CreateNodeView(NodeBase node, Point location)
        {
            Type nodeType = node.GetType();
            
            // 查找注册的NodeView类型
            if (nodeViewMappings.TryGetValue(nodeType, out Type viewType))
            {
                return (NodeView)Activator.CreateInstance(viewType, node, location);
            }

            // 如果没有找到对应的NodeView，使用默认的NodeView
            return new DefaultNodeView(node,location);
        }
    }
} 