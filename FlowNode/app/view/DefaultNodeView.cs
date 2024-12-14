using System.Drawing;
using FlowNode.node;
using System;
namespace FlowNode.app.view
{
    public class DefaultNodeView : NodeView
    {
        public DefaultNodeView(NodeBase node, Point location) : base(node, location)
        {
        }

        protected override void InitializeControls()
        {
            // 基础实现不添加任何控件
        }

    }
} 