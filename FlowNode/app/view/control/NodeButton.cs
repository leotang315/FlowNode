using System;
using System.Drawing;
using System.Windows.Forms;

namespace FlowNode.app.view
{
    public class NodeButton : NodeControl
    {
        public event EventHandler Click;
        public string Text { get; set; }
        private bool isPressed;
        private bool isHovered;

        public NodeButton(NodeView parentNode, string name, string text) : base(parentNode, name)
        {
            Text = text;
        }

        public override void Paint(Graphics g)
        {
            if (!Visible) return;

            // 绘制按钮背景
            Color backgroundColor = !Enabled ? Color.FromArgb(60, 60, 60) :
                                  isPressed ? Color.FromArgb(0, 122, 204) :
                                  isHovered ? Color.FromArgb(63, 63, 70) :
                                            Color.FromArgb(45, 45, 48);

            using (var brush = new SolidBrush(backgroundColor))
            {
                g.FillRectangle(brush, Bounds);
            }

            // 绘制边框
            using (var pen = new Pen(Color.FromArgb(100, 100, 100)))
            {
                g.DrawRectangle(pen, Bounds);
            }

            // 绘制文本
            using (var brush = new SolidBrush(Enabled ? Color.White : Color.Gray))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(Text, SystemFonts.DefaultFont, brush, Bounds, format);
            }
        }

        public override void OnMouseDown(Point location, MouseButtons button)
        {
            if (!Enabled || button != MouseButtons.Left) return;
            isPressed = true;
            ParentNode?.Invalidate();
        }

        public override void OnMouseUp(Point location, MouseButtons button)
        {
            if (!Enabled || button != MouseButtons.Left) return;
            if (isPressed && Bounds.Contains(location))
            {
                Click?.Invoke(this, EventArgs.Empty);
            }
            isPressed = false;
            ParentNode?.Invalidate();
        }

        public override void OnMouseMove(Point location)
        {
            bool newHovered = Bounds.Contains(location);
            if (newHovered != isHovered)
            {
                isHovered = newHovered;
                ParentNode?.Invalidate();
            }
        }
    }
} 