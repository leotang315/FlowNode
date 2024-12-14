using FlowNode.node;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace FlowNode.app.view
{
    public abstract class NodeView
    {
        public NodeBase Node { get; protected set; }
        public Rectangle Bounds { get; set; }
        public Dictionary<Pin, Rectangle> PinBounds { get; protected set; }
        public List<NodeControl> Controls { get; protected set; }

        protected NodeView(NodeBase node, Point location)
        {
            Node = node;
            Bounds = new Rectangle(location, new Size(200, 120));
            PinBounds = new Dictionary<Pin, Rectangle>();
            Controls = new List<NodeControl>();
            
            InitializeControls();
            UpdatePinLocations();
        }

        // 子类重写此方法来初始化自己的控件
        protected abstract void InitializeControls();

        // 绘制节点
        public virtual void Paint(Graphics g)
        {
            // 绘制节点背景
            using (var brush = new SolidBrush(Color.FromArgb(70, 70, 70)))
            {
                g.FillRectangle(brush, Bounds);
            }

            // 绘制节点头部
            var headerRect = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, 25);
            using (var brush = new SolidBrush(Color.FromArgb(90, 90, 90)))
            {
                g.FillRectangle(brush, headerRect);
            }

            // 绘制标题
            using (var brush = new SolidBrush(Color.White))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(Node.Name, SystemFonts.DefaultFont, brush, headerRect, format);
            }

            // 绘制边框
            using (var pen = new Pen(Color.FromArgb(100, 100, 100)))
            {
                g.DrawRectangle(pen, Bounds);
            }

            // 绘制引脚
            DrawPins(g);

            // 绘制控件
            foreach (var control in Controls)
            {
                control.Paint(g);
            }
        }

        protected virtual void DrawPins(Graphics g)
        {
            foreach (var pin in Node.Pins)
            {
                if (PinBounds.TryGetValue(pin, out Rectangle pinRect))
                {
                    Color pinColor = pin.pinType == PinType.Execute ? 
                        Color.FromArgb(255, 128, 0) : Color.FromArgb(0, 120, 255);

                    // 绘制引脚
                    using (var brush = new SolidBrush(pinColor))
                    {
                        g.FillEllipse(brush, pinRect);
                    }

                    // 绘制引脚名称
                    using (var brush = new SolidBrush(Color.White))
                    {
                        var textRect = new Rectangle(
                            pin.direction == PinDirection.Input ? pinRect.Right + 5 : pinRect.Left - 100,
                            pinRect.Top - 4,
                            100,
                            20);
                        var format = new StringFormat
                        {
                            Alignment = pin.direction == PinDirection.Input ? 
                                StringAlignment.Near : StringAlignment.Far,
                            LineAlignment = StringAlignment.Center
                        };
                        g.DrawString(pin.Name, SystemFonts.DefaultFont, brush, textRect, format);
                    }
                }
            }
        }

        public virtual void UpdatePinLocations()
        {
            PinBounds.Clear();

            var inputExecPins = Node.Pins.Where(p => p.direction == PinDirection.Input && p.pinType == PinType.Execute).ToList();
            var inputDataPins = Node.Pins.Where(p => p.direction == PinDirection.Input && p.pinType == PinType.Data).ToList();
            var outputExecPins = Node.Pins.Where(p => p.direction == PinDirection.Output && p.pinType == PinType.Execute).ToList();
            var outputDataPins = Node.Pins.Where(p => p.direction == PinDirection.Output && p.pinType == PinType.Data).ToList();
            var inputPins = inputExecPins.Concat(inputDataPins).ToList();
            var outputPins = outputExecPins.Concat(outputDataPins).ToList();

            // 布局输入引脚
            for (int i = 0; i < inputPins.Count; i++)
            {
                int y = Bounds.Top + 30 + (i * 25);
                PinBounds[inputPins[i]] = new Rectangle(
                    Bounds.Left - 8,
                    y - 4,
                    8,
                    8);
            }

            // 布局输出引脚
            for (int i = 0; i < outputPins.Count; i++)
            {
                int y = Bounds.Top + 30 + (i * 25);
                PinBounds[outputPins[i]] = new Rectangle(
                    Bounds.Right,
                    y - 4,
                    8,
                    8);
            }

            UpdateControlsLayout();
        }

        protected virtual void UpdateControlsLayout()
        {
            //int currentY = Bounds.Top + 30;

            //foreach (var control in Controls)
            //{
            //    if (control is NodeLabel)
            //    {
            //        control.Bounds = new Rectangle(Bounds.X + 10, currentY, Bounds.Width - 20, 20);
            //        currentY += 25;
            //    }
            //    else if (control is NodeButton)
            //    {
            //        control.Bounds = new Rectangle(Bounds.X + 10, currentY, Bounds.Width - 20, 25);
            //        currentY += 30;
            //    }
            //    else if (control is NodeProgressBar)
            //    {
            //        control.Bounds = new Rectangle(Bounds.X + 10, currentY, Bounds.Width - 20, 15);
            //        currentY += 20;
            //    }
            //}

            //// 调整节点高度
            //Bounds = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, Math.Max(120, currentY - Bounds.Top + 10));
        }

        public void AddControl(NodeControl control)
        {
            control.ParentNode = this;
            Controls.Add(control);
            UpdateControlsLayout();
        }

        // 处理鼠标事件
        public virtual bool HandleMouseDown(Point location, MouseButtons button)
        {
            foreach (var control in Controls)
            {
                if (control.Bounds.Contains(location))
                {
                    control.OnMouseDown(location, button);
                    return true;
                }
            }
            return false;
        }

        public virtual void HandleMouseUp(Point location, MouseButtons button)
        {
            foreach (var control in Controls)
            {
                control.OnMouseUp(location, button);
            }
        }

        public virtual void HandleMouseMove(Point location)
        {
            foreach (var control in Controls)
            {
                control.OnMouseMove(location);
            }
        }
    }
} 