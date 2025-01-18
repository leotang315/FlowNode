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
using System.Drawing.Drawing2D;
using FlowNode.app.command;
using FlowNode.app.serialization;
using FlowNode.app.view;
using System.Xml.Linq;
namespace FlowNode
{
    public partial class NodeEditor : UserControl
    {
        private NodeManager nodeManager = new NodeManager();
        private CommandManager commandManager = new CommandManager();
        private Dictionary<INode, NodeView> nodeViews = new Dictionary<INode, NodeView>();
        private NodeSerializationService serializationService;
        private EditorState currentState;


        private Point panOffset;   // 用于画布平移
        private float zoom = 1.0f; // 用于画布缩放

        private NodeView selectedNodeView;
        private Connector selectedConnector;
        private Pin selectedPin;
        private Pin hoveredPin;

        private HashSet<NodeView> selectedNodes = new HashSet<NodeView>();
        public HashSet<NodeView> SelectedNodes => selectedNodes;

        public NodeEditor()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.UserPaint, true);

            BackColor = Color.FromArgb(40, 40, 40);
            currentState = new IdleState(this);  // 初始化为空闲状态

            this.MouseWheel += NodeEditor_MouseWheel;
            serializationService = new NodeSerializationService(nodeManager, nodeViews);
        }

        public NodeManager NodeManager => nodeManager;

        public CommandManager CommandManager => commandManager;

        public void AddNode(NodeBase node, Point location)
        {
            var compositeCommand = new CompositeCommand();
            compositeCommand.AddCommand(new AddNodeDataCommand(nodeManager, node));
            var nodeView = NodeViewFactory.CreateNodeView(node, location);
            nodeView.Parent = this;
            compositeCommand.AddCommand(new AddNodeViewCommand(nodeViews, nodeView, location));
            commandManager.ExecuteCommand(compositeCommand);
            Invalidate();
        }

        public void RemoveNode(NodeBase node)
        {
            var compositeCommand = new CompositeCommand();
            compositeCommand.AddCommand(new RemoveNodeViewCommand(nodeViews, node));
            compositeCommand.AddCommand(new RemoveNodeDataCommand(nodeManager, node));
            commandManager.ExecuteCommand(compositeCommand);
            Invalidate();
        }

        /// <summary>
        /// 添加连接器
        /// </summary>
        public void AddConnector(Pin sourcePin, Pin targetPin)
        {
            var compositeCommand = new CompositeCommand();
            compositeCommand.AddCommand(new AddConnectorDataCommand(nodeManager, sourcePin, targetPin));
            commandManager.ExecuteCommand(compositeCommand);
            Invalidate();
        }

        /// <summary>
        /// 移除连接器
        /// </summary>
        public void RemoveConnector(Connector connector)
        {
            var compositeCommand = new CompositeCommand();
            compositeCommand.AddCommand(new RemoveConnectorDataCommand(nodeManager, connector));
            commandManager.ExecuteCommand(compositeCommand);
            Invalidate();
        }

        /// <summary>
        /// 移动节点
        /// </summary>
        public void MoveNode(NodeView nodeView, Point oldLocation, Point newLocation)
        {
            var compositeCommand = new CompositeCommand();
            compositeCommand.AddCommand(new MoveNodeViewCommand(nodeView, oldLocation, newLocation));
            commandManager.ExecuteCommand(compositeCommand);
            Invalidate();
        }

        public Point ScreenToNode(Point screenPos)
        {
            return new Point(
                (int)((screenPos.X - panOffset.X) / zoom),
                (int)((screenPos.Y - panOffset.Y) / zoom)
            );
        }

        public (NodeView nodeView, Pin pin, Connector connector) HitTest(Point location)
        {


            // 检查节点和引脚
            foreach (var nodeView in nodeViews.Values)
            {
                // 检查是否点击了引脚
                foreach (var pinPair in nodeView.PinBounds)
                {
                    if (pinPair.Value.Contains(location))
                        return (nodeView, pinPair.Key, null);
                }

                // 检查是否点击了节点本身
                if (nodeView.Bounds.Contains(location))
                    return (nodeView, null, null);
            }

            // 检查是否点击了连接器
            foreach (var connector in nodeManager.getConnectors())
            {
                if (IsPointOnConnector(location, connector))
                {
                    return (null, null, connector);
                }
            }
            return (null, null, null);
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

        public void Undo()
        {
            commandManager.Undo();
            Invalidate();
        }

        public void Redo()
        {
            commandManager.Redo();
            Invalidate();
        }

        public void SaveToFile(string filePath)
        {
            try
            {
                serializationService.SaveToFile(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void LoadFromFile(string filePath)
        {
            try
            {
                serializationService.LoadFromFile(filePath);
                Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void ChangeState(EditorState newState)
        {
            currentState = newState;
            Console.WriteLine(currentState.getName());
            Invalidate();
        }



        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            selectedNodeView?.HandleKeyPress(e);
            currentState.OnKeyPress(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            currentState.OnKeyDown(e);

            // 处理撤销 (Ctrl+Z)
            if (e.Control && e.KeyCode == Keys.Z)
            {
                if (commandManager.CanUndo)
                {
                    commandManager.Undo();
                    Invalidate();
                }
                e.Handled = true;
                return;
            }

            // 处理重做 (Ctrl+Y)
            if (e.Control && e.KeyCode == Keys.Y)
            {
                if (commandManager.CanRedo)
                {
                    commandManager.Redo();
                    Invalidate();
                }
                e.Handled = true;
                return;
            }

            // 处理删除选中节点 (Delete)
            if (e.KeyCode == Keys.Delete && selectedNodeView != null)
            {
                var compositeCommand = new CompositeCommand();
                foreach (var nodeView in selectedNodes.ToList())
                {
                    compositeCommand.AddCommand(new RemoveNodeViewCommand(nodeViews, nodeView.Node));
                    compositeCommand.AddCommand(new RemoveNodeDataCommand(nodeManager, nodeView.Node));
                }
                commandManager.ExecuteCommand(compositeCommand);
                selectedNodes.Clear();
                selectedNodeView = null;
                Invalidate();
            }
            selectedNodeView?.HandleKeyDown(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // 确保控件可以接收键盘输入
            this.Focus();

            // 先检查是否点击了节点上的控件
            if (selectedNodeView?.HandleMouseDown(ScreenToNode(e.Location), e.Button) == true)
            {
                return;
            }

            currentState.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            // 先处理节点控件的鼠标移动
            selectedNodeView?.HandleMouseMove(ScreenToNode(e.Location));

            currentState.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            // 先处理节点控件的鼠标释放
            selectedNodeView?.HandleMouseUp(ScreenToNode(e.Location), e.Button);

            currentState.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 应用缩放和平移
            g.TranslateTransform(panOffset.X, panOffset.Y);
            g.ScaleTransform(zoom, zoom);

            // 绘制网格
            DrawGrid(g);
            DrawConnectors(g);
            DrawNodes(g);

            // 让当前状态绘制其特定内容
            currentState.OnPaint(g);
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

        private void DrawNode(Graphics g, NodeView nodeView)
        {
            nodeView.Paint(g);
            //// 绘制选中节点的边框
            //if (selectedNodeView == nodeView)
            //{
            //    // 选中状态 - 用亮色边框和更粗的线条   // 绘制带圆角的矩形
            //    using (var pen = new Pen(Color.FromArgb(0, 120, 215), 2))
            //    using (var path = CreateRoundedRectangle(nodeView.Bounds, 3))
            //    {
            //        g.DrawPath(pen, path);
            //    }
            //}

            //// 遍历所有选中的节点
            //foreach (var node in SelectedNodes)
            //{
            //    // 绘制选中节点的边框
            //    using (var pen = new Pen(Color.FromArgb(0, 120, 215), 2))
            //    using (var path = CreateRoundedRectangle(node.Bounds, 3))
            //    {
            //        g.DrawPath(pen, path);
            //    }
            //}

            //if (isConnecting)
            //{
            //    foreach (var pinPair in nodeView.PinBounds)
            //    {
            //        var pin = pinPair.Key;
            //        var bounds = pinPair.Value;
            //        if (hoveredPin == pin)
            //        {
            //            var pinColor = CanConnect(selectedPin, pin) ? Color.FromArgb(0, 120, 215) : Color.FromArgb(255, 0, 0);
            //            using (var glowBrush = new SolidBrush(Color.FromArgb(100, pinColor)))
            //            {
            //                var glowRect = bounds;
            //                glowRect.Inflate(5, 5);
            //                g.FillEllipse(glowBrush, glowRect);
            //            }
            //        }
            //    }

            //}
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

            // 计算贝塞尔曲线的控制点
            float tangentLength = Math.Min(100, Math.Abs(endPoint.X - startPoint.X) * 0.5f);
            Point control1 = new Point(startPoint.X + (int)tangentLength, startPoint.Y);
            Point control2 = new Point(endPoint.X - (int)tangentLength, endPoint.Y);

            // 如果是选中的连接器，先绘制发光效果
            if (selectedConnector == connector)
            {
                using (var glowPen = new Pen(Color.FromArgb(100, lineColor), 6))
                {
                    g.DrawBezier(glowPen, startPoint, control1, control2, endPoint);
                }
            }

            // 绘制主连接线
            using (Pen pen = new Pen(lineColor, selectedConnector == connector ? 3 : 2))
            {
                g.DrawBezier(pen, startPoint, control1, control2, endPoint);
            }
        }

        private bool IsPointOnConnector(Point point, Connector connector)
        {
            if (!nodeViews.TryGetValue(connector.src.host, out NodeView srcView) ||
                !nodeViews.TryGetValue(connector.dst.host, out NodeView dstView))
                return false;

            if (!srcView.PinBounds.TryGetValue(connector.src, out Rectangle srcPinRect) ||
                !dstView.PinBounds.TryGetValue(connector.dst, out Rectangle dstPinRect))
                return false;

            Point startPoint = new Point(srcPinRect.Right, srcPinRect.Top + srcPinRect.Height / 2);
            Point endPoint = new Point(dstPinRect.Left, dstPinRect.Top + dstPinRect.Height / 2);

            // 计算贝塞尔曲线的控制点
            float tangentLength = Math.Min(100, Math.Abs(endPoint.X - startPoint.X) * 0.5f);
            Point control1 = new Point(startPoint.X + (int)tangentLength, startPoint.Y);
            Point control2 = new Point(endPoint.X - (int)tangentLength, endPoint.Y);

            // 检查点到曲线的距离
            return IsPointNearBezier(point, startPoint, control1, control2, endPoint, 5);
        }

        private bool IsPointNearBezier(Point point, Point start, Point control1, Point control2, Point end, float threshold)
        {
            // 简化的距离检查：将曲线分成多个线段进行检查
            const int segments = 20;
            Point prev = start;

            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                Point current = CalculateBezierPoint(t, start, control1, control2, end);

                // 检查点到线段的距离
                if (DistanceToLineSegment(point, prev, current) <= threshold)
                    return true;

                prev = current;
            }
            return false;
        }

        private Point CalculateBezierPoint(float t, Point start, Point control1, Point control2, Point end)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            float x = uuu * start.X +
                     3 * uu * t * control1.X +
                     3 * u * tt * control2.X +
                     ttt * end.X;

            float y = uuu * start.Y +
                     3 * uu * t * control1.Y +
                     3 * u * tt * control2.Y +
                     ttt * end.Y;

            return new Point((int)x, (int)y);
        }

        private float DistanceToLineSegment(Point point, Point lineStart, Point lineEnd)
        {
            float dx = lineEnd.X - lineStart.X;
            float dy = lineEnd.Y - lineStart.Y;

            if (dx == 0 && dy == 0)
                return (float)Math.Sqrt(Math.Pow(point.X - lineStart.X, 2) + Math.Pow(point.Y - lineStart.Y, 2));

            float t = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t));

            float projX = lineStart.X + t * dx;
            float projY = lineStart.Y + t * dy;

            return (float)Math.Sqrt(Math.Pow(point.X - projX, 2) + Math.Pow(point.Y - projY, 2));
        }

        private bool CanConnect(Pin source, Pin target)
        {
            if (source == null || target == null)
                return false;

            // 检查方向和引脚类型
            bool basicCheck = source.direction != target.direction && // 方向相反
                             source.pinType == target.pinType;    // 类型相同

            if (!basicCheck) return false;

            // 如果是执行类型的引脚，不需要检查数据类型
            if (source.pinType == PinType.Execute)
                return true;

            // 确保source是输出引脚，target是输入引脚
            Pin outputPin, inputPin;
            if (source.direction == PinDirection.Output)
            {
                outputPin = source;
                inputPin = target;
            }
            else
            {
                outputPin = target;
                inputPin = source;
            }
            return PinTypeValidator.AreTypesCompatible(outputPin.dataType, inputPin.dataType);
        }

        private GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();

            // 左上角
            path.AddArc(bounds.X, bounds.Y, radius * 2, radius * 2, 180, 90);
            // 右上角
            path.AddArc(bounds.Right - radius * 2, bounds.Y, radius * 2, radius * 2, 270, 90);
            // 右下角
            path.AddArc(bounds.Right - radius * 2, bounds.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            // 左下角
            path.AddArc(bounds.X, bounds.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);

            path.CloseFigure();
            return path;
        }

        //private void DrawConnectingLine(Graphics g)
        //{
        //    // 如果没有悬停的引脚，使用鼠标位置作为终点
        //    Point endPoint = hoveredPin != null ?
        //        GetPinConnectionPoint(hoveredPin) : connectingEnd;

        //    // 计算贝塞尔曲线的控制点
        //    float tangentLength = Math.Min(100, Math.Abs(endPoint.X - connectingStart.X) * 0.5f);
        //    Point control1 = new Point(connectingStart.X + (int)tangentLength, connectingStart.Y);
        //    Point control2 = new Point(endPoint.X - (int)tangentLength, endPoint.Y);

        //    // 检查连接兼容性
        //    bool isCompatible = hoveredPin != null && CanConnect(selectedPin, hoveredPin);

        //    // 根据兼容性选择颜色和样式
        //    if (hoveredPin != null && !isCompatible)
        //    {
        //        // 不兼容的连接显示为红色虚线
        //        using (var pen = new Pen(Color.Red, 2))
        //        {
        //            pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
        //            g.DrawBezier(pen, connectingStart, control1, control2, endPoint);
        //        }
        //    }
        //    else
        //    {
        //        // 正常连接显示为白色
        //        using (var pen = new Pen(Color.White, 2))
        //        {
        //            pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
        //            g.DrawBezier(pen, connectingStart, control1, control2, endPoint);
        //        }
        //    }
        //}

        private Point GetPinConnectionPoint(Pin pin)
        {
            if (nodeViews.TryGetValue(pin.host, out NodeView nodeView) &&
                nodeView.PinBounds.TryGetValue(pin, out Rectangle pinRect))
            {
                return new Point(
                    pin.direction == PinDirection.Input ? pinRect.Left : pinRect.Right,
                    pinRect.Top + pinRect.Height / 2
                );
            }
            return Point.Empty;
        }

        public Point PanOffset
        {
            get => panOffset;
            set => panOffset = value;
        }

        public IReadOnlyDictionary<INode, NodeView> NodeViews => nodeViews;

        public void ClearSelection()
        {
            selectedNodes.Clear();
        }

        public void AddToSelection(NodeView nodeView)
        {
            selectedNodes.Add(nodeView);
        }

        public void RemoveFromSelection(NodeView nodeView)
        {
            selectedNodes.Remove(nodeView);
        }

        private void DrawConnectors(Graphics g)
        {
            foreach (var connector in nodeManager.getConnectors())
            {
                DrawConnector(g, connector);
            }
        }

        private void DrawNodes(Graphics g)
        {
            foreach (var nodeView in nodeViews.Values)
            {
                DrawNode(g, nodeView);

                // 绘制选中高亮
                if (selectedNodes.Contains(nodeView))
                {
                    using (var pen = new Pen(Color.FromArgb(0, 120, 215), 2))
                    using (var path = CreateRoundedRectangle(nodeView.Bounds, 3))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }
        }

        public void SelectNode(NodeView nodeView, bool clearOthers = true)
        {
            if (clearOthers)
            {
                selectedNodes.Clear();
            }
            selectedNodes.Add(nodeView);
            selectedNodeView = nodeView;
            Invalidate();
        }

        public void DeselectAll()
        {
            selectedNodes.Clear();
            selectedNodeView = null;
            Invalidate();
        }

        public void StartConnecting(Pin pin)
        {
            ChangeState(new ConnectingState(this, pin));
        }

        public void StartDragging(NodeView nodeView, Point mousePos)
        {
            ChangeState(new DraggingNodeState(this, nodeView, mousePos));
        }

        public void StartPanning(Point startPos)
        {
            ChangeState(new PanningState(this, startPos));
        }

        public void StartSelecting(Point startPos)
        {
            ChangeState(new SelectingState(this, startPos));
        }
    }



}
