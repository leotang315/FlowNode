using FlowNode.node;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlowNode
{
    public partial class NodeEditor : UserControl
    {
        private NodeManager nodeManager;
        private Dictionary<INode, NodeView> nodeViews;
        private NodeView selectedNodeView;
        private Pin selectedPin;
        private Point dragStart;
        private bool isDragging;
        private bool isConnecting;
        private Point connectingStart;
        private Point connectingEnd;
        private Point panOffset;
        private float zoom = 1.0f;
        private Pin hoveredPin;

        public NodeEditor()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.UserPaint, true);

            nodeManager = new NodeManager();
            nodeViews = new Dictionary<INode, NodeView>();
            BackColor = Color.FromArgb(40, 40, 40);

            // 启用鼠标滚轮
            this.MouseWheel += NodeEditor_MouseWheel;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 应用缩放和平移
            g.TranslateTransform(panOffset.X, panOffset.Y);
            g.ScaleTransform(zoom, zoom);

            // 绘制网格
            DrawGrid(g);

            // 绘制连接线
            foreach (var connector in nodeManager.getConnectors())
            {
                DrawConnector(g, connector);
            }

            // 绘制正在创建的连接线
            if (isConnecting)
            {
                using (Pen pen = new Pen(Color.White, 2))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    g.DrawLine(pen, connectingStart, connectingEnd);
                }
            }

            // 绘制节点
            foreach (var nodeView in nodeViews.Values)
            {
                DrawNode(g, nodeView);
            }
        }

        private void DrawGrid(Graphics g)
        {
            // 计算可见区域的边界（考虑缩放和平移）
            var visibleRect = new RectangleF(
                -panOffset.X / zoom,
                -panOffset.Y / zoom,
                Width / zoom,
                Height / zoom
            );

            // 网格大小
            var gridSize = 20;

            // 计算网格起始点（确保网格覆盖整个可见区域）
            int startX = (int)(Math.Floor(visibleRect.Left / gridSize) * gridSize);
            int startY = (int)(Math.Floor(visibleRect.Top / gridSize) * gridSize);
            int endX = (int)(Math.Ceiling(visibleRect.Right / gridSize) * gridSize);
            int endY = (int)(Math.Ceiling(visibleRect.Bottom / gridSize) * gridSize);

            using (Pen pen = new Pen(Color.FromArgb(60, 60, 60), 1))
            {
                // 绘制垂直线
                for (int x = startX; x <= endX; x += gridSize)
                {
                    g.DrawLine(pen, x, startY, x, endY);
                }
                // 绘制水平线
                for (int y = startY; y <= endY; y += gridSize)
                {
                    g.DrawLine(pen, startX, y, endX, y);
                }
            }
        }
        // private void DrawGrid(Graphics g)
        // {
        //     var gridSize = 20;
        //     using (Pen pen = new Pen(Color.FromArgb(60, 60, 60), 1))
        //     {
        //         for (int x = 0; x < Width; x += gridSize)
        //         {
        //             g.DrawLine(pen, x, 0, x, Height);
        //         }
        //         for (int y = 0; y < Height; y += gridSize)
        //         {
        //             g.DrawLine(pen, 0, y, Width, y);
        //         }
        //     }
        // }

        private void DrawNode(Graphics g, NodeView nodeView)
        {
            var rect = nodeView.Bounds;
            var headerRect = new Rectangle(rect.X, rect.Y, rect.Width, 25);

            // 绘制节点背景
            using (var brush = new SolidBrush(Color.FromArgb(70, 70, 70)))
            {
                g.FillRectangle(brush, rect);
            }

            // 绘制节点头部
            using (var brush = new SolidBrush(Color.FromArgb(90, 90, 90)))
            {
                g.FillRectangle(brush, headerRect);
            }

            // 绘制边框
            using (var pen = new Pen(Color.FromArgb(100, 100, 100), 1))
            {
                g.DrawRectangle(pen, rect);
            }

            // 绘制标题
            using (var brush = new SolidBrush(Color.White))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(nodeView.Node.Name, Font, brush, headerRect, format);
            }

            // 绘制引脚
            foreach (var pin in nodeView.Node.Pins)
            {
                if (nodeView.PinBounds.TryGetValue(pin, out Rectangle pinRect))
                {
                    Color pinColor = pin.pinType == PinType.Execute ? Color.FromArgb(255, 128, 0) : Color.FromArgb(0, 120, 255);

                    // 判断是否需要高亮显示
                    bool shouldHighlight = false;
                    if (isConnecting && selectedPin != null && pin != selectedPin)
                    {
                        // 检查连接兼容性
                        if (selectedPin.direction != pin.direction && // 方向相反
                            selectedPin.pinType == pin.pinType &&    // 类型相同
                            ((selectedPin.direction == PinDirection.Output && pin.direction == PinDirection.Input) ||
                             (selectedPin.direction == PinDirection.Input && pin.direction == PinDirection.Output)))
                        {
                            shouldHighlight = true;
                        }
                    }

                    // 高亮效果
                    if (shouldHighlight)
                    {
                        // 绘制外发光效果
                        using (var glowBrush = new SolidBrush(Color.FromArgb(100, pinColor)))
                        {
                            var glowRect = pinRect;
                            glowRect.Inflate(4, 4);
                            g.FillEllipse(glowBrush, glowRect);
                        }
                    }

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
                            Alignment = pin.direction == PinDirection.Input ? StringAlignment.Near : StringAlignment.Far,
                            LineAlignment = StringAlignment.Center
                        };
                        g.DrawString(pin.Name, Font, brush, textRect, format);
                    }
                }
            }
        }

        private void DrawConnector(Graphics g, Connector connector)
        {
            if (!nodeViews.TryGetValue(connector.src.host, out NodeView srcView) ||
                !nodeViews.TryGetValue(connector.dst.host, out NodeView dstView))
                return;

            if (!srcView.PinBounds.TryGetValue(connector.src, out Rectangle srcPinRect) ||
                !dstView.PinBounds.TryGetValue(connector.dst, out Rectangle dstPinRect))
                return;

            Point startPoint = new Point(srcPinRect.Right, srcPinRect.Top + srcPinRect.Height / 2);
            Point endPoint = new Point(dstPinRect.Left, dstPinRect.Top + dstPinRect.Height / 2);

            Color lineColor = connector.src.pinType == PinType.Execute ?
                Color.FromArgb(255, 128, 0) : Color.FromArgb(0, 120, 255);

            using (Pen pen = new Pen(lineColor, 2))
            {
                // 计算贝塞尔曲线的控制点
                float tangentLength = Math.Min(100, Math.Abs(endPoint.X - startPoint.X) * 0.5f);
                Point control1 = new Point(startPoint.X + (int)tangentLength, startPoint.Y);
                Point control2 = new Point(endPoint.X - (int)tangentLength, endPoint.Y);

                // 绘制连接线
                g.DrawBezier(pen, startPoint, control1, control2, endPoint);

                // 如果是正在创建的连接线，且类型不匹配，显示错误提示
                if (isConnecting && !CanConnect(selectedPin, hoveredPin))
                {
                    using (var errorPen = new Pen(Color.Red, 2))
                    {
                        errorPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                        g.DrawBezier(errorPen, startPoint, control1, control2, endPoint);
                    }
                }
            }
        }

        public void AddNode(NodeBase node, Point location)
        {
            //node.init();
            nodeManager.addNode(node);
            nodeViews[node] = new NodeView(node, location);
            Invalidate();
        }

        public void RemoveNode(NodeBase node)
        {
            throw new NotImplementedException();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            var mousePos = ScreenToNode(e.Location);
            if (e.Button == MouseButtons.Left)
            {
                var (nodeView, pin) = HitTest(mousePos);
                selectedNodeView = nodeView;
                selectedPin = pin;

                if (selectedPin != null)
                {
                    isConnecting = true;
                    connectingStart = new Point(
                        selectedPin.direction == PinDirection.Input ?
                            nodeView.PinBounds[selectedPin].Left :
                            nodeView.PinBounds[selectedPin].Right,
                        nodeView.PinBounds[selectedPin].Top + nodeView.PinBounds[selectedPin].Height / 2);
                    connectingEnd = mousePos;
                }
                else if (selectedNodeView != null)
                {
                    isDragging = true;
                    dragStart = mousePos;
                }
            }
            else if (e.Button == MouseButtons.Middle)
            {
                isDragging = true;
                dragStart = e.Location;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var mousePos = ScreenToNode(e.Location);

            // 更新悬停的引脚
            var (_, pin) = HitTest(mousePos);
            if (hoveredPin != pin)
            {
                hoveredPin = pin;
                Invalidate();
            }

            if (isDragging)
            {
                if (e.Button == MouseButtons.Middle)
                {
                    panOffset.X += e.X - dragStart.X;
                    panOffset.Y += e.Y - dragStart.Y;
                    dragStart = e.Location;
                }
                else if (selectedNodeView != null)
                {
                    var dx = mousePos.X - dragStart.X;
                    var dy = mousePos.Y - dragStart.Y;
                    selectedNodeView.Bounds = new Rectangle(
                        selectedNodeView.Bounds.X + dx,
                        selectedNodeView.Bounds.Y + dy,
                        selectedNodeView.Bounds.Width,
                        selectedNodeView.Bounds.Height
                    );
                    selectedNodeView.UpdatePinLocations();
                    dragStart = mousePos;
                }
                Invalidate();
            }
            else if (isConnecting)
            {
                connectingEnd = mousePos;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            var mousePos = ScreenToNode(e.Location);

            if (isConnecting)
            {
                var (nodeView, pin) = HitTest(mousePos);
                if (pin != null && selectedPin != null)
                {
                    try
                    {
                        if (selectedPin.direction == PinDirection.Output)
                            nodeManager.addConnector(selectedPin, pin);
                        else
                            nodeManager.addConnector(pin, selectedPin);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            isDragging = false;
            isConnecting = false;
            selectedNodeView = null;
            selectedPin = null;
            Invalidate();
        }

        private void NodeEditor_MouseWheel(object sender, MouseEventArgs e)
        {
            float oldZoom = zoom;
            if (e.Delta > 0)
                zoom = Math.Min(zoom * 1.1f, 5.0f);
            else
                zoom = Math.Max(zoom / 1.1f, 0.2f);

            // 调整平移以保持鼠标位置不变
            if (oldZoom != zoom)
            {
                Point mousePos = e.Location;
                panOffset.X = mousePos.X - (int)((mousePos.X - panOffset.X) * (zoom / oldZoom));
                panOffset.Y = mousePos.Y - (int)((mousePos.Y - panOffset.Y) * (zoom / oldZoom));
                Invalidate();
            }
        }

        public Point ScreenToNode(Point screenPos)
        {
            return new Point(
                (int)((screenPos.X - panOffset.X) / zoom),
                (int)((screenPos.Y - panOffset.Y) / zoom)
            );
        }

        private (NodeView nodeView, Pin pin) HitTest(Point location)
        {
            foreach (var nodeView in nodeViews.Values)
            {
                // 检查是否点击了引脚
                foreach (var pinPair in nodeView.PinBounds)
                {
                    if (pinPair.Value.Contains(location))
                        return (nodeView, pinPair.Key);
                }

                // 检查是否点击了节点本身
                if (nodeView.Bounds.Contains(location))
                    return (nodeView, null);
            }
            return (null, null);
        }

        public void ExecuteFlow()
        {
            try
            {
                nodeManager.run();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Execution Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool CanConnect(Pin source, Pin target)
        {
            if (source == null || target == null)
                return false;

            return source.direction != target.direction && // 方向相反
                   source.pinType == target.pinType &&    // 类型相同
                   ((source.direction == PinDirection.Output && target.direction == PinDirection.Input) ||
                    (source.direction == PinDirection.Input && target.direction == PinDirection.Output));
        }


    }


    public class NodeView
    {
        public NodeBase Node { get; set; }
        public Rectangle Bounds { get; set; }
        public Dictionary<Pin, Rectangle> PinBounds { get; private set; }

        public NodeView(NodeBase node, Point location)
        {
            Node = node;
            Bounds = new Rectangle(location, new Size(200, 120));
            PinBounds = new Dictionary<Pin, Rectangle>();
            UpdatePinLocations();
        }

        public void UpdatePinLocations()
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
    }
}
