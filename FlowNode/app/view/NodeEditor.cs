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

        private Connector selectedConnector;

        // 执行可视化：当前正在执行的节点，以及每步高亮的停顿时长
        private INode currentExecutingNode;
        public int ExecutionStepDelayMs { get; set; } = 200;

        // 断点集合，以及"从断点继续时跳过一次断点检查"的标记
        private readonly HashSet<INode> breakpoints = new HashSet<INode>();
        private bool skipBreakpointOnce;

        // 执行出错时为 true：保留出错节点高亮并以红色显示
        private bool executionError;

        /// <summary>执行过程中的日志消息，供外部日志面板订阅。</summary>
        public event Action<string> ExecutionLog;

        private HashSet<NodeView> selectedNodes = new HashSet<NodeView>();
        public HashSet<NodeView> SelectedNodes => selectedNodes;

        /// <summary>
        /// 选中节点集合发生变化时触发，供属性面板等外部 UI 订阅。
        /// </summary>
        public event Action SelectionChanged;

        /// <summary>图内容被修改时触发（增删改节点/连线/布局等），供未保存提示等订阅。</summary>
        public event Action GraphChanged;

        private void NotifyGraphChanged()
        {
            GraphChanged?.Invoke();
        }

        // 复制/粘贴使用的内存剪贴板
        private List<ClipboardNode> clipboardNodes;
        private List<ClipboardConnector> clipboardConnectors;
        private int pasteCount;

        private class ClipboardNode
        {
            public string NodePath;
            public string Name;
            public bool IsAutoRun;
            public Point Location;
            public List<PropertyData> Properties = new List<PropertyData>();
            public List<PinData> Pins = new List<PinData>();
            public string VarName;
            public string VarTypeName;
            public bool? VarIsSet;
        }

        private class ClipboardConnector
        {
            public int SrcNodeIndex;
            public string SrcPinName;
            public int DstNodeIndex;
            public string DstPinName;
        }

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
            commandManager.AfterModify += NotifyGraphChanged;
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
            if (nodeViews.TryGetValue(node, out var view))
                InvalidateNodeViewsWithConnectors(new[] { view });
            else
                Invalidate();
        }

        public void RemoveNode(NodeBase node)
        {
            if (nodeViews.TryGetValue(node, out var view))
                InvalidateNodeViewsWithConnectors(new[] { view });

            var compositeCommand = new CompositeCommand();
            compositeCommand.AddCommand(new RemoveNodeViewCommand(nodeViews, node));
            compositeCommand.AddCommand(new RemoveNodeDataCommand(nodeManager, node));
            commandManager.ExecuteCommand(compositeCommand);
        }

        /// <summary>
        /// 添加连接器
        /// </summary>
        public void AddConnector(Pin sourcePin, Pin targetPin)
        {
            var compositeCommand = new CompositeCommand();
            compositeCommand.AddCommand(new AddConnectorDataCommand(nodeManager, sourcePin, targetPin));
            commandManager.ExecuteCommand(compositeCommand);

            var dirty = RectangleF.Empty;
            if (nodeViews.TryGetValue(sourcePin.host, out var srcView))
                dirty = EditorViewport.Union(dirty, GetNodeViewsDirtyRect(new[] { srcView }));
            if (nodeViews.TryGetValue(targetPin.host, out var dstView))
                dirty = EditorViewport.Union(dirty, GetNodeViewsDirtyRect(new[] { dstView }));
            if (dirty.Width <= 0 || dirty.Height <= 0)
                Invalidate();
            else
                InvalidateWorldRect(dirty);
        }

        /// <summary>
        /// 移除连接器
        /// </summary>
        public void RemoveConnector(Connector connector)
        {
            var dirty = RectangleF.Empty;
            if (nodeViews.TryGetValue(connector.src.host, out var srcView))
                dirty = EditorViewport.Union(dirty, GetNodeViewsDirtyRect(new[] { srcView }));
            if (nodeViews.TryGetValue(connector.dst.host, out var dstView))
                dirty = EditorViewport.Union(dirty, GetNodeViewsDirtyRect(new[] { dstView }));

            var compositeCommand = new CompositeCommand();
            compositeCommand.AddCommand(new RemoveConnectorDataCommand(nodeManager, connector));
            commandManager.ExecuteCommand(compositeCommand);

            if (dirty.Width <= 0 || dirty.Height <= 0)
                Invalidate();
            else
                InvalidateWorldRect(dirty);
        }

        /// <summary>
        /// 移动节点
        /// </summary>
        public void MoveNode(NodeView nodeView, Point oldLocation, Point newLocation)
        {
            var dirty = GetNodeViewsDirtyRect(new[] { nodeView });

            var compositeCommand = new CompositeCommand();
            compositeCommand.AddCommand(new MoveNodeViewCommand(nodeView, oldLocation, newLocation));
            commandManager.ExecuteCommand(compositeCommand);

            dirty = EditorViewport.Union(dirty, GetNodeViewsDirtyRect(new[] { nodeView }));
            InvalidateWorldRect(dirty);
        }

        /// <summary>删除当前选中的全部节点（含关联连线），单次 Undo。</summary>
        public void DeleteSelection()
        {
            if (selectedNodes.Count == 0)
                return;

            var views = selectedNodes.ToList();
            var dirty = GetNodeViewsDirtyRect(views);

            var compositeCommand = new CompositeCommand();
            foreach (var nodeView in views)
            {
                compositeCommand.AddCommand(new RemoveNodeViewCommand(nodeViews, nodeView.Node));
                compositeCommand.AddCommand(new RemoveNodeDataCommand(nodeManager, nodeView.Node));
            }

            commandManager.ExecuteCommand(compositeCommand);
            ClearSelection();
            InvalidateWorldRect(dirty);
        }

        public Point ScreenToNode(Point screenPos)
        {
            return new Point(
                (int)((screenPos.X - panOffset.X) / zoom),
                (int)((screenPos.Y - panOffset.Y) / zoom)
            );
        }

        /// <summary>将世界坐标脏区转换为控件客户区并局部重绘；无法计算时回退全屏 Invalidate。</summary>
        public void InvalidateWorldRect(RectangleF worldRect)
        {
            var clientRect = EditorViewport.WorldToClientRect(worldRect, zoom, panOffset, Size);
            if (clientRect.IsEmpty)
                Invalidate();
            else
                Invalidate(clientRect);
        }

        public RectangleF GetNodeViewsDirtyRect(IEnumerable<NodeView> views)
        {
            var viewSet = new HashSet<NodeView>(views);
            var rects = new List<RectangleF>();
            foreach (var view in views)
            {
                rects.Add(EditorViewport.ExpandNodeBounds(view.Bounds));
            }

            foreach (var connector in nodeManager.getConnectors())
            {
                if (!nodeViews.TryGetValue(connector.src.host, out var srcView) ||
                    !nodeViews.TryGetValue(connector.dst.host, out var dstView))
                    continue;

                if (!viewSet.Contains(srcView) && !viewSet.Contains(dstView))
                    continue;

                var connectorBounds = GetConnectorWorldBounds(connector);
                if (connectorBounds.Width > 0 && connectorBounds.Height > 0)
                    rects.Add(connectorBounds);
            }

            return EditorViewport.UnionAll(rects);
        }

        public void InvalidateNodeViewsWithConnectors(IEnumerable<NodeView> views)
        {
            var dirty = GetNodeViewsDirtyRect(views);
            if (dirty.Width <= 0 || dirty.Height <= 0)
                Invalidate();
            else
                InvalidateWorldRect(dirty);
        }

        public void InvalidateNode(INode node)
        {
            if (node != null && nodeViews.TryGetValue(node, out var view))
                InvalidateNodeViewsWithConnectors(new[] { view });
        }

        public RectangleF GetConnectorWorldBounds(Connector connector)
        {
            if (!nodeViews.TryGetValue(connector.src.host, out NodeView srcView) ||
                !nodeViews.TryGetValue(connector.dst.host, out NodeView dstView))
                return RectangleF.Empty;

            if (!srcView.PinBounds.TryGetValue(connector.src, out Rectangle srcPinRect) ||
                !dstView.PinBounds.TryGetValue(connector.dst, out Rectangle dstPinRect))
                return RectangleF.Empty;

            var startPoint = new Point(srcPinRect.Right, srcPinRect.Top + srcPinRect.Height / 2);
            var endPoint = new Point(dstPinRect.Left, dstPinRect.Top + dstPinRect.Height / 2);
            return EditorViewport.GetConnectorWorldBounds(startPoint, endPoint);
        }

        public void InvalidateConnector(Connector connector)
        {
            var bounds = GetConnectorWorldBounds(connector);
            if (bounds.Width <= 0 || bounds.Height <= 0)
                Invalidate();
            else
                InvalidateWorldRect(bounds);
        }

        public Point GetPinConnectionPoint(Pin pin)
        {
            if (nodeViews.TryGetValue(pin.host, out NodeView nodeView) &&
                nodeView.PinBounds.TryGetValue(pin, out Rectangle pinRect))
            {
                return new Point(
                    pin.direction == PinDirection.Input ? pinRect.Left : pinRect.Right,
                    pinRect.Top + pinRect.Height / 2);
            }

            return Point.Empty;
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

        /// <summary>
        /// 运行/继续执行。未在执行时会先校验并开始；已暂停于断点时则继续运行。
        /// </summary>
        public void ExecuteFlow()
        {
            if (nodeManager.IsRunning)
            {
                // 从断点继续：跳过当前停留的断点节点一次，避免立即再次命中
                skipBreakpointOnce = true;
                RunLoop();
                return;
            }

            // 执行前先做图校验，未通过则在日志中列出原因并中止
            var errors = nodeManager.Validate();
            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    ExecutionLog?.Invoke("[校验] " + error);
                }
                MessageBox.Show("图校验未通过，无法执行：\n\n" + string.Join("\n", errors),
                    "无法执行", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ResetExecutionState();
            LogWarnings();
            AttachExecutionHandlers();
            try
            {
                nodeManager.BeginRun();
            }
            catch (Exception ex)
            {
                ExecutionLog?.Invoke("[错误] " + ex.Message);
                FinishExecution();
                return;
            }

            skipBreakpointOnce = false;
            RunLoop();
        }

        /// <summary>
        /// 单步执行：执行下一个节点。未在执行时会先校验并开始（进入暂停/单步模式）。
        /// </summary>
        public void StepExecution()
        {
            if (!nodeManager.IsRunning)
            {
                var errors = nodeManager.Validate();
                if (errors.Count > 0)
                {
                    foreach (var error in errors)
                    {
                        ExecutionLog?.Invoke("[校验] " + error);
                    }
                    MessageBox.Show("图校验未通过，无法执行：\n\n" + string.Join("\n", errors),
                        "无法执行", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ResetExecutionState();
                LogWarnings();
                AttachExecutionHandlers();
                try
                {
                    nodeManager.BeginRun();
                }
                catch (Exception ex)
                {
                    ExecutionLog?.Invoke("[错误] " + ex.Message);
                    FinishExecution();
                    return;
                }
            }

            try
            {
                nodeManager.Step();
            }
            catch (Exception ex)
            {
                executionError = true;
                ExecutionLog?.Invoke("[错误] " + ex.Message);
                MessageBox.Show($"Execution Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                nodeManager.StopRun(true);
                FinishExecution();
                return;
            }

            if (!nodeManager.IsRunning)
            {
                FinishExecution();
            }
            else
            {
                Invalidate();
            }
        }

        /// <summary>主动停止当前执行。</summary>
        public void StopExecution()
        {
            if (!nodeManager.IsRunning)
                return;
            nodeManager.StopRun();
            executionError = false;
            currentExecutingNode = null;
            FinishExecution();
        }

        private void RunLoop()
        {
            try
            {
                while (nodeManager.HasMoreSteps)
                {
                    var next = nodeManager.PeekNext();
                    if (!skipBreakpointOnce && breakpoints.Contains(next))
                    {
                        // 命中断点：暂停，保留执行状态，等待继续或单步
                        currentExecutingNode = next;
                        ExecutionLog?.Invoke($"命中断点: {(next as NodeBase)?.Name}");
                        Invalidate();
                        Update();
                        return;
                    }

                    skipBreakpointOnce = false;
                    nodeManager.Step();

                    if (ExecutionStepDelayMs > 0)
                    {
                        System.Threading.Thread.Sleep(ExecutionStepDelayMs);
                    }
                }
                FinishExecution();
            }
            catch (Exception ex)
            {
                executionError = true;
                ExecutionLog?.Invoke("[错误] " + ex.Message);
                MessageBox.Show($"Execution Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                nodeManager.StopRun(true);
                FinishExecution();
            }
        }

        private void AttachExecutionHandlers()
        {
            nodeManager.NodeExecuting += OnNodeExecuting;
            nodeManager.Log += OnManagerLog;
        }

        private void FinishExecution()
        {
            nodeManager.NodeExecuting -= OnNodeExecuting;
            nodeManager.Log -= OnManagerLog;
            // 出错时保留高亮停在出错节点上，便于定位
            if (!executionError)
            {
                currentExecutingNode = null;
            }
            skipBreakpointOnce = false;
            Invalidate();
        }

        private void ResetExecutionState()
        {
            executionError = false;
            currentExecutingNode = null;
        }

        private void LogWarnings()
        {
            foreach (var warning in nodeManager.ValidateWarnings())
            {
                ExecutionLog?.Invoke("[警告] " + warning);
            }
        }

        /// <summary>
        /// 切换某节点的断点状态。
        /// </summary>
        public void ToggleBreakpoint(NodeView nodeView)
        {
            if (nodeView?.Node == null)
                return;
            if (!breakpoints.Remove(nodeView.Node))
            {
                breakpoints.Add(nodeView.Node);
                ExecutionLog?.Invoke($"添加断点: {nodeView.Node.Name}");
            }
            else
            {
                ExecutionLog?.Invoke($"移除断点: {nodeView.Node.Name}");
            }
            Invalidate();
        }

        private void OnNodeExecuting(INode node)
        {
            var dirty = RectangleF.Empty;
            if (currentExecutingNode != null && nodeViews.TryGetValue(currentExecutingNode, out var previousView))
                dirty = EditorViewport.ExpandNodeBounds(previousView.Bounds);

            currentExecutingNode = node;

            if (nodeViews.TryGetValue(node, out var nextView))
                dirty = EditorViewport.Union(dirty, EditorViewport.ExpandNodeBounds(nextView.Bounds));

            if (dirty.Width <= 0 || dirty.Height <= 0)
                Invalidate();
            else
                InvalidateWorldRect(dirty);

            Update();
        }

        private void OnManagerLog(string message)
        {
            ExecutionLog?.Invoke(message);
        }

        /// <summary>向日志面板输出编辑器提示（连线失败等）。</summary>
        public void LogEditorMessage(string message)
        {
            ExecutionLog?.Invoke(message);
        }

        /// <summary>缩放并平移视口，使当前选中节点居中可见。</summary>
        public void ZoomToSelection()
        {
            if (selectedNodes.Count == 0)
                return;

            ZoomToBounds(GetCombinedBounds(selectedNodes));
        }

        /// <summary>缩放并平移视口，使全部节点适应画布。</summary>
        public void ZoomToFitAll()
        {
            if (nodeViews.Count == 0)
                return;

            ZoomToBounds(GetCombinedBounds(nodeViews.Values));
        }

        private static Rectangle GetCombinedBounds(IEnumerable<NodeView> views)
        {
            int left = int.MaxValue;
            int top = int.MaxValue;
            int right = int.MinValue;
            int bottom = int.MinValue;

            foreach (var view in views)
            {
                left = Math.Min(left, view.Bounds.Left);
                top = Math.Min(top, view.Bounds.Top);
                right = Math.Max(right, view.Bounds.Right);
                bottom = Math.Max(bottom, view.Bounds.Bottom);
            }

            if (left == int.MaxValue)
                return Rectangle.Empty;

            return Rectangle.FromLTRB(left, top, right, bottom);
        }

        private void ZoomToBounds(Rectangle bounds)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            const float padding = 48f;
            float availableW = Math.Max(1, Width - padding * 2);
            float availableH = Math.Max(1, Height - padding * 2);
            zoom = Math.Max(0.2f, Math.Min(5f, Math.Min(availableW / bounds.Width, availableH / bounds.Height)));

            float centerX = bounds.X + bounds.Width / 2f;
            float centerY = bounds.Y + bounds.Height / 2f;
            panOffset.X = (int)(Width / 2f - centerX * zoom);
            panOffset.Y = (int)(Height / 2f - centerY * zoom);
            Invalidate();
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

        /// <summary>当前图内容的 SHA256 指纹，供 dirty 智能比对。</summary>
        public string ComputeContentFingerprint()
        {
            return serializationService.ComputeContentFingerprint();
        }

        /// <summary>
        /// 清空当前图，回到空白文档状态（节点、连接、全局变量、选中、撤销历史全部清空）。
        /// </summary>
        public void NewGraph()
        {
            if (nodeManager.IsRunning)
            {
                nodeManager.StopRun(true);
                FinishExecution();
            }
            nodeManager.clear();
            nodeViews.Clear();
            commandManager.Clear();
            selectedConnector = null;
            breakpoints.Clear();
            executionError = false;
            currentExecutingNode = null;
            ClearSelection();
            Invalidate();
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
            currentState.OnKeyPress(e);
            foreach (var node in selectedNodes)
            {
                node.HandleKeyPress(e);
            }
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

            // 全选 (Ctrl+A)
            if (e.Control && e.KeyCode == Keys.A)
            {
                SelectAll();
                e.Handled = true;
                return;
            }

            // 切换断点 (F9)：对选中的单个节点
            if (e.KeyCode == Keys.F9)
            {
                if (selectedNodes.Count == 1)
                {
                    ToggleBreakpoint(selectedNodes.First());
                }
                e.Handled = true;
                return;
            }

            // 复制 (Ctrl+C)
            if (e.Control && e.KeyCode == Keys.C)
            {
                CopySelection();
                e.Handled = true;
                return;
            }

            // 粘贴 (Ctrl+V)
            if (e.Control && e.KeyCode == Keys.V)
            {
                PasteClipboard();
                e.Handled = true;
                return;
            }

            // 对齐与等距 (Ctrl+Shift+…)
            if (e.Control && e.Shift)
            {
                switch (e.KeyCode)
                {
                    case Keys.L:
                        AlignSelection(NodeAlignEdge.Left);
                        e.Handled = true;
                        return;
                    case Keys.R:
                        AlignSelection(NodeAlignEdge.Right);
                        e.Handled = true;
                        return;
                    case Keys.T:
                        AlignSelection(NodeAlignEdge.Top);
                        e.Handled = true;
                        return;
                    case Keys.B:
                        AlignSelection(NodeAlignEdge.Bottom);
                        e.Handled = true;
                        return;
                    case Keys.H:
                        DistributeSelectionHorizontally();
                        e.Handled = true;
                        return;
                    case Keys.J:
                    case Keys.V:
                        DistributeSelectionVertically();
                        e.Handled = true;
                        return;
                    case Keys.D0:
                    case Keys.NumPad0:
                        ZoomToSelection();
                        e.Handled = true;
                        return;
                }
            }

            // 适应全部节点 (Ctrl+0)
            if (e.Control && !e.Shift && e.KeyCode == Keys.D0)
            {
                ZoomToFitAll();
                e.Handled = true;
                return;
            }

            // 处理删除选中节点 (Delete)
            if (e.KeyCode == Keys.Delete)
            {
                DeleteSelection();
                e.Handled = true;
                return;
            }

            foreach (var node in selectedNodes)
            {
                node.HandleKeyDown(e);
            }

        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            currentState.OnMouseDown(e);
            foreach (var node in selectedNodes)
            {
                node.HandleMouseDown(ScreenToNode(e.Location), e.Button);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            currentState.OnMouseMove(e);
            foreach (var node in selectedNodes)
            {
                node.HandleMouseMove(ScreenToNode(e.Location));
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            currentState.OnMouseUp(e);
            foreach (var node in selectedNodes)
            {
                node.HandleMouseUp(ScreenToNode(e.Location), e.Button);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 应用缩放和平移
            g.TranslateTransform(panOffset.X, panOffset.Y);
            g.ScaleTransform(zoom, zoom);

            // 绘制基础元素
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

        /// <summary>
        /// 计算当前可见区域在世界坐标系下的矩形（考虑缩放和平移），用于视口裁剪。
        /// </summary>
        private RectangleF GetVisibleWorldRect()
        {
            return new RectangleF(
                -panOffset.X / zoom,
                -panOffset.Y / zoom,
                Width / zoom,
                Height / zoom
            );
        }

        private void DrawGrid(Graphics g)
        {
            // 计算可见区域的边界（考虑缩放和平移）
            var visibleRect = GetVisibleWorldRect();

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

        private void DrawConnectors(Graphics g)
        {
            var visibleRect = GetVisibleWorldRect();

            foreach (var connector in nodeManager.getConnectors())
            {
                if (!nodeViews.TryGetValue(connector.src.host, out NodeView srcView) ||
                !nodeViews.TryGetValue(connector.dst.host, out NodeView dstView))
                    continue;

                // 视口裁剪：连线包围盒不在可见区域则跳过
                var connectorBounds = GetConnectorWorldBounds(connector);
                if (connectorBounds.Width <= 0 || connectorBounds.Height <= 0 ||
                    !visibleRect.IntersectsWith(connectorBounds))
                    continue;

                if (!srcView.PinBounds.TryGetValue(connector.src, out Rectangle srcPinRect) ||
                    !dstView.PinBounds.TryGetValue(connector.dst, out Rectangle dstPinRect))
                    continue;

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
        }

        private void DrawNodes(Graphics g)
        {
            var visibleRect = GetVisibleWorldRect();

            foreach (var nodeView in nodeViews.Values)
            {
                // 视口裁剪：节点（含断点圆点的外扩边距）不在可见区域则跳过
                var cullBounds = nodeView.Bounds;
                cullBounds.Inflate(8, 8);
                if (!visibleRect.IntersectsWith(cullBounds))
                    continue;

                nodeView.Paint(g);

                // 绘制当前执行节点高亮：正常为绿色发光，出错节点为红色发光
                if (currentExecutingNode != null && nodeView.Node == currentExecutingNode)
                {
                    var bounds = nodeView.Bounds;
                    bounds.Inflate(2, 2);
                    Color coreColor = executionError ? Color.FromArgb(235, 60, 40) : Color.FromArgb(0, 230, 80);
                    using (var glowPen = new Pen(Color.FromArgb(120, coreColor), 6))
                    using (var glowPath = CreateRoundedRectangle(bounds, 4))
                    {
                        g.DrawPath(glowPen, glowPath);
                    }
                    using (var pen = new Pen(coreColor, 2))
                    using (var path = CreateRoundedRectangle(bounds, 4))
                    {
                        g.DrawPath(pen, path);
                    }
                }

                // 绘制选中高亮
                if (selectedNodes.Contains(nodeView))
                {
                    using (var pen = new Pen(Color.FromArgb(0, 120, 215), 2))
                    using (var path = CreateRoundedRectangle(nodeView.Bounds, 3))
                    {
                        g.DrawPath(pen, path);
                    }
                }

                // 绘制断点标记（左上角红色圆点）
                if (breakpoints.Contains(nodeView.Node))
                {
                    var b = nodeView.Bounds;
                    var dotRect = new Rectangle(b.Left - 6, b.Top - 6, 12, 12);
                    using (var brush = new SolidBrush(Color.FromArgb(220, 40, 40)))
                    using (var pen = new Pen(Color.White, 1))
                    {
                        g.FillEllipse(brush, dotRect);
                        g.DrawEllipse(pen, dotRect);
                    }
                }
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

        public Point PanOffset
        {
            get => panOffset;
            set => panOffset = value;
        }

        public IReadOnlyDictionary<INode, NodeView> NodeViews => nodeViews;

        public void ClearSelection()
        {
            if (selectedNodes.Count == 0)
                return;
            selectedNodes.Clear();
            SelectionChanged?.Invoke();
        }

        public void AddToSelection(NodeView nodeView)
        {
            if (selectedNodes.Add(nodeView))
                SelectionChanged?.Invoke();
        }

        public void RemoveFromSelection(NodeView nodeView)
        {
            if (selectedNodes.Remove(nodeView))
                SelectionChanged?.Invoke();
        }

        /// <summary>
        /// 选中画布中的全部节点。
        /// </summary>
        public void SelectAll()
        {
            bool changed = false;
            foreach (var nodeView in nodeViews.Values)
            {
                changed |= selectedNodes.Add(nodeView);
            }
            if (changed)
            {
                SelectionChanged?.Invoke();
                Invalidate();
            }
        }

        /// <summary>
        /// 复制当前选中的节点及它们之间的内部连线到内存剪贴板。
        /// </summary>
        public void CopySelection()
        {
            if (selectedNodes.Count == 0)
                return;

            clipboardNodes = new List<ClipboardNode>();
            clipboardConnectors = new List<ClipboardConnector>();
            pasteCount = 0;

            var indexMap = new Dictionary<NodeView, int>();
            foreach (var nodeView in selectedNodes)
            {
                indexMap[nodeView] = clipboardNodes.Count;
                clipboardNodes.Add(new ClipboardNode
                {
                    NodePath = nodeView.Node.NodePath,
                    Name = nodeView.Node.Name,
                    IsAutoRun = nodeView.Node.IsAutoRun,
                    Location = nodeView.Bounds.Location,
                    Properties = NodeSnapshotHelper.CaptureProperties(nodeView.Node),
                    Pins = NodeSnapshotHelper.CapturePins(nodeView.Node)
                });
                if (NodeFactory.TryGetVarNodeInfo(nodeView.Node, out var varName, out var varType, out var isSet))
                {
                    var last = clipboardNodes[clipboardNodes.Count - 1];
                    last.VarName = varName;
                    last.VarTypeName = varType.AssemblyQualifiedName;
                    last.VarIsSet = isSet;
                }
            }

            // 仅复制两端都在选区内的连线
            var selectedHosts = new HashSet<INode>(selectedNodes.Select(n => n.Node));
            foreach (var connector in nodeManager.getConnectors())
            {
                if (selectedHosts.Contains(connector.src.host) &&
                    selectedHosts.Contains(connector.dst.host) &&
                    nodeViews.TryGetValue(connector.src.host, out var srcView) &&
                    nodeViews.TryGetValue(connector.dst.host, out var dstView))
                {
                    clipboardConnectors.Add(new ClipboardConnector
                    {
                        SrcNodeIndex = indexMap[srcView],
                        SrcPinName = connector.src.Name,
                        DstNodeIndex = indexMap[dstView],
                        DstPinName = connector.dst.Name
                    });
                }
            }
        }

        /// <summary>
        /// 将剪贴板内容粘贴到画布，整体偏移并选中粘贴出的节点，作为一次可撤销操作。
        /// </summary>
        public void PasteClipboard()
        {
            if (clipboardNodes == null || clipboardNodes.Count == 0)
                return;

            // 每次粘贴递增偏移，避免与上次粘贴完全重叠
            pasteCount++;
            int offset = 30 * pasteCount;

            ClearSelection();

            var createdViews = new NodeView[clipboardNodes.Count];
            using (commandManager.BeginCommandGroup())
            {
                for (int i = 0; i < clipboardNodes.Count; i++)
                {
                    var cn = clipboardNodes[i];
                    NodeBase node;
                    try
                    {
                        if (!string.IsNullOrEmpty(cn.VarName) && cn.VarIsSet.HasValue)
                        {
                            node = NodeFactory.CreateVarNodeFromInfo(cn.VarName, cn.VarTypeName, cn.VarIsSet.Value);
                        }
                        else if (!string.IsNullOrEmpty(cn.NodePath))
                        {
                            node = NodeFactory.CreateNode(cn.NodePath);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"粘贴时创建节点失败: {ex.Message}");
                        continue;
                    }

                    node.Name = cn.Name;
                    node.IsAutoRun = cn.IsAutoRun;
                    NodeSnapshotHelper.Apply(node, cn.Properties, cn.Pins);

                    var location = new Point(cn.Location.X + offset, cn.Location.Y + offset);
                    var nodeView = NodeViewFactory.CreateNodeView(node, location);
                    nodeView.Parent = this;

                    commandManager.ExecuteCommand(new AddNodeDataCommand(nodeManager, node));
                    commandManager.ExecuteCommand(new AddNodeViewCommand(nodeViews, nodeView, location));

                    createdViews[i] = nodeView;
                }

                foreach (var cc in clipboardConnectors)
                {
                    var srcView = createdViews[cc.SrcNodeIndex];
                    var dstView = createdViews[cc.DstNodeIndex];
                    if (srcView == null || dstView == null)
                        continue;

                    var srcPin = srcView.Node.findPin(cc.SrcPinName);
                    var dstPin = dstView.Node.findPin(cc.DstPinName);
                    if (srcPin != null && dstPin != null)
                    {
                        commandManager.ExecuteCommand(new AddConnectorDataCommand(nodeManager, srcPin, dstPin));
                    }
                }
            }

            foreach (var view in createdViews)
            {
                if (view != null)
                    selectedNodes.Add(view);
            }
            SelectionChanged?.Invoke();
            Invalidate();
        }

        public void SetSelectedConnector(Connector connector)
        {
            selectedConnector = connector;
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

        /// <summary>将当前选中的多个节点按指定边对齐（至少 2 个节点）。</summary>
        public void AlignSelection(NodeAlignEdge edge)
        {
            if (selectedNodes.Count < 2)
                return;

            ApplyLayoutMoves(NodeEditorLayout.Align(selectedNodes.ToList(), edge));
        }

        /// <summary>将当前选中的多个节点水平等距排列（至少 3 个节点）。</summary>
        public void DistributeSelectionHorizontally()
        {
            if (selectedNodes.Count < 3)
                return;

            ApplyLayoutMoves(NodeEditorLayout.DistributeHorizontally(selectedNodes.ToList()));
        }

        /// <summary>将当前选中的多个节点垂直等距排列（至少 3 个节点）。</summary>
        public void DistributeSelectionVertically()
        {
            if (selectedNodes.Count < 3)
                return;

            ApplyLayoutMoves(NodeEditorLayout.DistributeVertically(selectedNodes.ToList()));
        }

        private void ApplyLayoutMoves(Dictionary<NodeView, Point> moves)
        {
            if (moves == null || moves.Count == 0)
                return;

            var dirty = GetNodeViewsDirtyRect(moves.Keys);

            using (commandManager.BeginCommandGroup())
            {
                foreach (var pair in moves)
                {
                    var oldLocation = pair.Key.Bounds.Location;
                    commandManager.ExecuteCommand(new MoveNodeViewCommand(pair.Key, oldLocation, pair.Value));
                }
            }

            dirty = EditorViewport.Union(dirty, GetNodeViewsDirtyRect(moves.Keys));
            InvalidateWorldRect(dirty);
        }
    }



}
