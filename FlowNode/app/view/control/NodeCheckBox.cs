using System;
using System.Drawing;
using System.Windows.Forms;

namespace FlowNode.app.view
{
    public class NodeCheckBox : NodeControl
    {
        public bool Checked { get; set; }
        public string Text { get; set; }
        public event EventHandler<bool> CheckedChanged;
        private bool isHovered;

        public NodeCheckBox(NodeView parentNode, string name, string text, bool initialState = false) 
            : base(parentNode, name)
        {
            Text = text;
            Checked = initialState;
        }

        public override void Paint(Graphics g)
        {
            if (!Visible) return;

            // 绘制复选框
            Rectangle checkRect = new Rectangle(Bounds.X, Bounds.Y + 2, 16, 16);
            
            // 绘制背景
            using (var brush = new SolidBrush(Color.FromArgb(30, 30, 30)))
            {
                g.FillRectangle(brush, checkRect);
            }

            // 绘制边框
            using (var pen = new Pen(isHovered ? Color.FromArgb(0, 122, 204) : Color.FromArgb(100, 100, 100)))
            {
                g.DrawRectangle(pen, checkRect);
            }

            // 如果被选中，绘制勾选标记
            if (Checked)
            {
                using (var pen = new Pen(Color.White, 2))
                {
                    g.DrawLine(pen,
                        checkRect.X + 3, checkRect.Y + 8,
                        checkRect.X + 6, checkRect.Y + 11);
                    g.DrawLine(pen,
                        checkRect.X + 6, checkRect.Y + 11,
                        checkRect.X + 13, checkRect.Y + 4);
                }
            }

            // 绘制文本
            using (var brush = new SolidBrush(Color.White))
            {
                var textRect = new Rectangle(checkRect.Right + 4, Bounds.Y,
                                           Bounds.Width - checkRect.Width - 4, Bounds.Height);
                g.DrawString(Text, SystemFonts.DefaultFont, brush, textRect);
            }
        }

        public override void OnMouseDown(Point location, MouseButtons button)
        {
            if (!Enabled || button != MouseButtons.Left) return;
            Checked = !Checked;
            CheckedChanged?.Invoke(this, Checked);
            ParentNode?.Invalidate();
        }

        public override void OnMouseUp(Point location, MouseButtons button) { }

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