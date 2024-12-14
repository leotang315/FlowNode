using System.Drawing;
using System.Windows.Forms;

namespace FlowNode.app.view
{
    public class NodeLabel : NodeControl
    {
        public string Text { get; set; }
        public Color TextColor { get; set; } = Color.White;
        public ContentAlignment TextAlign { get; set; } = ContentAlignment.MiddleLeft;

        public NodeLabel(NodeView parentNode, string name, string text) : base(parentNode, name)
        {
            Text = text;
        }

        public override void Paint(Graphics g)
        {
            if (!Visible) return;

            using (var brush = new SolidBrush(TextColor))
            using (var format = new StringFormat())
            {
                // 设置文本对齐方式
                switch (TextAlign)
                {
                    case ContentAlignment.TopLeft:
                        format.Alignment = StringAlignment.Near;
                        format.LineAlignment = StringAlignment.Near;
                        break;
                    case ContentAlignment.TopCenter:
                        format.Alignment = StringAlignment.Center;
                        format.LineAlignment = StringAlignment.Near;
                        break;
                    case ContentAlignment.TopRight:
                        format.Alignment = StringAlignment.Far;
                        format.LineAlignment = StringAlignment.Near;
                        break;
                    case ContentAlignment.MiddleLeft:
                        format.Alignment = StringAlignment.Near;
                        format.LineAlignment = StringAlignment.Center;
                        break;
                    case ContentAlignment.MiddleCenter:
                        format.Alignment = StringAlignment.Center;
                        format.LineAlignment = StringAlignment.Center;
                        break;
                    case ContentAlignment.MiddleRight:
                        format.Alignment = StringAlignment.Far;
                        format.LineAlignment = StringAlignment.Center;
                        break;
                    case ContentAlignment.BottomLeft:
                        format.Alignment = StringAlignment.Near;
                        format.LineAlignment = StringAlignment.Far;
                        break;
                    case ContentAlignment.BottomCenter:
                        format.Alignment = StringAlignment.Center;
                        format.LineAlignment = StringAlignment.Far;
                        break;
                    case ContentAlignment.BottomRight:
                        format.Alignment = StringAlignment.Far;
                        format.LineAlignment = StringAlignment.Far;
                        break;
                }

                g.DrawString(Text, SystemFonts.DefaultFont, brush, Bounds, format);
            }
        }

        public override void OnMouseDown(Point location, MouseButtons button) { }
        public override void OnMouseUp(Point location, MouseButtons button) { }
        public override void OnMouseMove(Point location) { }
    }
} 