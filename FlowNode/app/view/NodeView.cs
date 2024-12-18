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
        private Rectangle bounds;
        public Rectangle Bounds
        {
            get => bounds;
            set
            {
                bounds = value;
                UpdatePinLocations();
                UpdateControlsLayout();
            }
        }
        public Dictionary<Pin, Rectangle> PinBounds { get; protected set; }
        public List<NodeControl> Controls { get; protected set; }
        public NodeEditor Parent { get; set; }
        private NodeControl focusedControl;

        public Graphics CreateGraphics()
        {
            return Parent?.CreateGraphics();
        }

        public void Invalidate()
        {
            // 通知父控件重绘
            Parent?.Invalidate(Bounds);
        }

        protected NodeView(NodeBase node, Point location)
        {
            Node = node;
            bounds = new Rectangle(location, new Size(200, 120));
            PinBounds = new Dictionary<Pin, Rectangle>();
            Controls = new List<NodeControl>();

            InitializeControls();
            UpdatePinLocations();
            UpdateControlsLayout();
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

        protected virtual void UpdatePinLocations()
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

        }

        protected virtual void UpdateControlsLayout()
        {
            // 计算输入和输出引脚的数量，用于确定节点的最小高度
            int maxPins = Math.Max(
                Node.Pins.Count(p => p.direction == PinDirection.Input),
                Node.Pins.Count(p => p.direction == PinDirection.Output)
            );

            // 从标题栏下方开始布局
            int currentY = bounds.Y + 30;
            const int MARGIN = 10;
            const int SPACING = 5;

            // 遍历所有控件，从上到下布局
            foreach (var control in Controls)
            {
                // 设置控件位置
                control.Bounds = new Rectangle(
                    bounds.X + MARGIN,                    // X位置：左边距
                    currentY,                             // Y位置：当前高度
                    bounds.Width - (MARGIN * 2),          // 宽度：节点宽度减去左右边距
                    control.Bounds.Height                 // 保持控件原有高度
                );

                // 更新下一个控件的Y位置
                currentY = control.Bounds.Bottom + SPACING;
            }

            // 计算所需的总高度
            int requiredHeight = Math.Max(
                maxPins * 25 + 40,                       // 引脚所需的最小高度
                currentY - bounds.Y + MARGIN              // 控件所需的高度
            );

            // 更新节点高度，保持宽度不变
            bounds = new Rectangle(
                bounds.Location,
                new Size(bounds.Width, Math.Max(120, requiredHeight))
            );
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
            NodeControl newFocusedControl = null;
            foreach (var control in Controls)
            {
                if (control.Visible && control.Enabled && control.Bounds.Contains(location))
                {
                    newFocusedControl = control;
                    control.Focus();
                    control.OnMouseDown(location, button);
                    return true;
                }
            }

            // 处理焦点变化
            if (focusedControl != newFocusedControl)
            {
                focusedControl?.LoseFocus();
                focusedControl = newFocusedControl;
            }

            return false;
        }

        public virtual void HandleMouseUp(Point location, MouseButtons button)
        {
            foreach (var control in Controls)
            {
                if (control.Visible && control.Enabled && control.Bounds.Contains(location))
                {
                    control.OnMouseUp(location, button);
                    return;
                }
            }
        }

        public virtual void HandleMouseMove(Point location)
        {
            // 从后向前遍历控件
            foreach (var control in Controls)
            {
                if (control.Visible && control.Enabled)
                {
                    control.OnMouseMove(location);
                    return;
                }
            }
        }

        // 添加键盘事件处理
        public virtual void HandleKeyPress(KeyPressEventArgs e)
        {
            var focusedControl = Controls.FirstOrDefault(c => 
                c.Visible && 
                c.Enabled && 
                c.IsFocused);
            if (focusedControl != null)
            {
                focusedControl.OnKeyPress(e);
            }
        }

        public virtual void HandleKeyDown(KeyEventArgs e)
        {
            var focusedControl = Controls.FirstOrDefault(c => 
                c.Visible && 
                c.Enabled &&
                c.IsFocused);

            if (focusedControl != null)
            {
                focusedControl.OnKeyDown(e);
            }
        }

        public virtual void HandleKeyUp(KeyEventArgs e)
        {
            var focusedControl = Controls.FirstOrDefault(c => 
                c.Visible && 
                c.Enabled &&
                c.IsFocused);

            if (focusedControl != null)
            {
                focusedControl.OnKeyUp(e);
            }
        }

        public virtual void HandleMouseWheel(MouseEventArgs e)
        {
            var focusedControl = Controls.FirstOrDefault(c => 
                c.Visible && 
                c.Enabled &&
                c.IsFocused);

            if (focusedControl != null)
            {
                focusedControl.OnMouseWheel(e);
            }
        }

        public void BringToFront(NodeControl control)
        {
            if (Controls.Remove(control))
            {
                Controls.Add(control);
            }
        }
    }
}