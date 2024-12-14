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

        protected override void UpdateControlsLayout()
        {
            // 基础实现不需要特殊的控件布局
            int maxPinY = 0;
            foreach (var pinRect in PinBounds.Values)
            {
                maxPinY = Math.Max(maxPinY, pinRect.Bottom);
            }

            // 设置节点高度为最低引脚位置加上边距
            Bounds = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, Math.Max(120, maxPinY + 20));
        }
    }
} 