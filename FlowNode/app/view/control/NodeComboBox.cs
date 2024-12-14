using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;

namespace FlowNode.app.view
{
    public class NodeComboBox : NodeControl
    {
        public List<string> Items { get; private set; }
        public int SelectedIndex { get; set; } = -1;
        public string SelectedItem => SelectedIndex >= 0 && SelectedIndex < Items.Count ? 
                                    Items[SelectedIndex] : null;
        public event EventHandler<int> SelectedIndexChanged;
        
        private bool isDropped;
        private bool isHovered;
        private int hoveredItemIndex = -1;
        private Rectangle dropButtonBounds;
        private Rectangle dropDownBounds;

        public NodeComboBox(NodeView parentNode, string name) : base(parentNode, name)
        {
            Items = new List<string>();
        }

        public override void Paint(Graphics g)
        {
            if (!Visible) return;

            // 绘制主框体背景
            using (var brush = new SolidBrush(Color.FromArgb(30, 30, 30)))
            {
                g.FillRectangle(brush, Bounds);
            }

            // 绘制边框
            using (var pen = new Pen(Color.FromArgb(100, 100, 100)))
            {
                g.DrawRectangle(pen, Bounds);
            }

            // 绘制选中项
            if (SelectedIndex >= 0 && SelectedIndex < Items.Count)
            {
                using (var brush = new SolidBrush(Color.White))
                {
                    var textRect = new Rectangle(Bounds.X + 3, Bounds.Y + 2,
                                               Bounds.Width - 20, Bounds.Height - 4);
                    g.DrawString(Items[SelectedIndex], SystemFonts.DefaultFont, brush, textRect);
                }
            }

            // 绘制下拉按钮
            dropButtonBounds = new Rectangle(Bounds.Right - 17, Bounds.Y + 2,
                                           15, Bounds.Height - 4);
            using (var brush = new SolidBrush(Color.FromArgb(63, 63, 70)))
            {
                g.FillRectangle(brush, dropButtonBounds);
            }

            // 绘制三角形
            using (var brush = new SolidBrush(Color.White))
            {
                Point[] triangle = new Point[]
                {
                    new Point(dropButtonBounds.X + 4, dropButtonBounds.Y + 6),
                    new Point(dropButtonBounds.X + 11, dropButtonBounds.Y + 6),
                    new Point(dropButtonBounds.X + 7, dropButtonBounds.Y + 11)
                };
                g.FillPolygon(brush, triangle);
            }

            // 如果下拉列表展开，绘制下拉列表
            if (isDropped)
            {
                dropDownBounds = new Rectangle(Bounds.X, Bounds.Bottom,
                                             Bounds.Width, Items.Count * 20);
                
                // 绘制下拉列表背景
                using (var brush = new SolidBrush(Color.FromArgb(45, 45, 48)))
                {
                    g.FillRectangle(brush, dropDownBounds);
                }

                // 绘制边框
                using (var pen = new Pen(Color.FromArgb(100, 100, 100)))
                {
                    g.DrawRectangle(pen, dropDownBounds);
                }

                // 绘制项目
                for (int i = 0; i < Items.Count; i++)
                {
                    var itemRect = new Rectangle(dropDownBounds.X, dropDownBounds.Y + i * 20,
                                               dropDownBounds.Width, 20);
                    
                    // 绘制悬停效果
                    if (i == hoveredItemIndex)
                    {
                        using (var brush = new SolidBrush(Color.FromArgb(63, 63, 70)))
                        {
                            g.FillRectangle(brush, itemRect);
                        }
                    }

                    // 绘制文本
                    using (var brush = new SolidBrush(Color.White))
                    {
                        g.DrawString(Items[i], SystemFonts.DefaultFont, brush,
                                   new Rectangle(itemRect.X + 3, itemRect.Y + 2,
                                               itemRect.Width - 6, itemRect.Height - 4));
                    }
                }
            }
        }

        public override void OnMouseDown(Point location, MouseButtons button)
        {
            if (!Enabled || button != MouseButtons.Left) return;

            if (dropButtonBounds.Contains(location) || Bounds.Contains(location))
            {
                isDropped = !isDropped;
                ParentNode?.Invalidate();
            }
            else if (isDropped && dropDownBounds.Contains(location))
            {
                int newIndex = (location.Y - dropDownBounds.Y) / 20;
                if (newIndex >= 0 && newIndex < Items.Count)
                {
                    SelectedIndex = newIndex;
                    SelectedIndexChanged?.Invoke(this, SelectedIndex);
                }
                isDropped = false;
                ParentNode?.Invalidate();
            }
        }

        public override void OnMouseUp(Point location, MouseButtons button) { }

        public override void OnMouseMove(Point location)
        {
            bool newHovered = Bounds.Contains(location) || 
                             (isDropped && dropDownBounds.Contains(location));
            
            // 计算悬停的项目索引
            if (isDropped && dropDownBounds.Contains(location))
            {
                hoveredItemIndex = (location.Y - dropDownBounds.Y) / 20;
                if (hoveredItemIndex >= Items.Count)
                {
                    hoveredItemIndex = -1;
                }
            }
            else
            {
                hoveredItemIndex = -1;
            }

            if (newHovered != isHovered)
            {
                isHovered = newHovered;
                ParentNode?.Invalidate();
            }
        }
    }
} 