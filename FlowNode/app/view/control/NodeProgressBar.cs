using System.Drawing;
using System.Windows.Forms;

namespace FlowNode.app.view
{
    public class NodeProgressBar : NodeControl
    {
        private int minimum = 0;
        private int maximum = 100;
        private int value = 0;

        public int Minimum
        {
            get => minimum;
            set
            {
                minimum = value;
                if (this.value < minimum) this.value = minimum;
                ParentNode?.Invalidate();
            }
        }

        public int Maximum
        {
            get => maximum;
            set
            {
                maximum = value;
                if (this.value > maximum) this.value = maximum;
                ParentNode?.Invalidate();
            }
        }

        public int Value
        {
            get => value;
            set
            {
                this.value = value;
                if (this.value < minimum) this.value = minimum;
                if (this.value > maximum) this.value = maximum;
                ParentNode?.Invalidate();
            }
        }

        public NodeProgressBar(NodeView parentNode, string name) : base(parentNode, name) { }

        public override void Paint(Graphics g)
        {
            if (!Visible) return;

            // 绘制背景
            using (var brush = new SolidBrush(Color.FromArgb(30, 30, 30)))
            {
                g.FillRectangle(brush, Bounds);
            }

            // 绘制边框
            using (var pen = new Pen(Color.FromArgb(100, 100, 100)))
            {
                g.DrawRectangle(pen, Bounds);
            }

            // 计算进度条宽度
            float progress = (float)(Value - Minimum) / (Maximum - Minimum);
            int progressWidth = (int)(Bounds.Width * progress);

            // 绘制进度条
            if (progressWidth > 0)
            {
                using (var brush = new SolidBrush(Color.FromArgb(0, 122, 204)))
                {
                    g.FillRectangle(brush, new Rectangle(Bounds.X, Bounds.Y,
                                                       progressWidth, Bounds.Height));
                }
            }
        }

        public override void OnMouseDown(Point location, MouseButtons button) { }
        public override void OnMouseUp(Point location, MouseButtons button) { }
        public override void OnMouseMove(Point location) { }
    }
} 