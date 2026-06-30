using FlowNode;
using FlowNode.app.serialization;
using FlowNode.app.view;
using FlowNode.node;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace FlowNode
{
    public partial class DemoForm : Form
    {

        private NodeEditor nodeEditor;
        private TreeView nodeTreeView;
        private VariableListControl variableListControl;
        private PropertyPanel propertyPanel;
        private DataViewControl dataView;
        private TextBox logTextBox;
        private string currentFilePath;
        private bool isDirty;
        private string cleanContentFingerprint;
        private const int MaxRecentFiles = 8;
        private readonly List<string> recentFiles = new List<string>();
        private ToolStripMenuItem recentFilesMenu;
        public DemoForm()
        {
            InitializeComponent();



            InitializeMenu();
            InitializeNodeEditor();
            InitializeNodeTreeView();
            //InitializeVariableListControl();

            InitializeDataView();
            InitializePropertyPanel();
            InitializeLogPanel();
            WireDirtyTracking();
            LoadRecentFiles();
            RefreshRecentMenu();
            UpdateTitle();
        }

        private static string RecentFilesStorePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlowNode", "recent.txt");

        private void WireDirtyTracking()
        {
            FormClosing += DemoForm_FormClosing;
            nodeEditor.GraphChanged += OnGraphChanged;
            CaptureCleanFingerprint();
        }

        private void CaptureCleanFingerprint()
        {
            cleanContentFingerprint = nodeEditor.ComputeContentFingerprint();
        }

        private void OnGraphChanged()
        {
            if (cleanContentFingerprint != null &&
                nodeEditor.ComputeContentFingerprint() == cleanContentFingerprint)
            {
                ClearDirty();
                return;
            }

            MarkDirty();
        }

        private void MarkDirty()
        {
            if (isDirty)
                return;

            isDirty = true;
            UpdateTitle();
        }

        private void ClearDirty()
        {
            if (!isDirty)
                return;

            isDirty = false;
            UpdateTitle();
        }

        /// <summary>若有未保存更改则提示；返回 Yes=继续（已保存或放弃），No=放弃，Cancel=取消操作。</summary>
        private DialogResult ConfirmDiscardChanges()
        {
            if (!isDirty)
                return DialogResult.Yes;

            var result = MessageBox.Show(
                "当前文档有未保存的更改，是否保存？",
                "未保存的更改",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                if (string.IsNullOrEmpty(currentFilePath))
                {
                    SaveFileAs();
                }
                else
                {
                    SaveFile();
                }

                return isDirty ? DialogResult.Cancel : DialogResult.Yes;
            }

            return result;
        }

        private void DemoForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (ConfirmDiscardChanges() == DialogResult.Cancel)
            {
                e.Cancel = true;
            }
        }

        private void InitializeLogPanel()
        {
            logTextBox = new TextBox
            {
                Dock = DockStyle.Bottom,
                Height = 120,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(25, 25, 25),
                ForeColor = Color.FromArgb(220, 220, 220),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(logTextBox);

            // 保持填充画布在最底层 z-order，确保日志面板停靠在底部、画布填充剩余区域
            nodeEditor.SendToBack();

            nodeEditor.ExecutionLog += AppendLog;
        }

        private void AppendLog(string message)
        {
            if (logTextBox == null)
                return;

            logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            // 执行期间界面被同步占用，主动刷新以实时显示
            logTextBox.Update();
        }

        private void InitializeMenu()
        {
            // 创建主工具栏
            ToolStrip toolStrip = new ToolStrip();

            // 创建文件下拉按钮
            var fileButton = new ToolStripDropDownButton("File");

            var newMenuItem = new ToolStripMenuItem("New");
            newMenuItem.Click += (s, e) => NewFile();
            fileButton.DropDownItems.Add(newMenuItem);

            var openMenuItem = new ToolStripMenuItem("Open");
            openMenuItem.Click += (s, e) => OpenFile();
            fileButton.DropDownItems.Add(openMenuItem);

            fileButton.DropDownItems.Add(new ToolStripSeparator());

            var saveMenuItem = new ToolStripMenuItem("Save");
            saveMenuItem.Click += (s, e) => SaveFile();
            fileButton.DropDownItems.Add(saveMenuItem);

            var saveAsMenuItem = new ToolStripMenuItem("Save As...");
            saveAsMenuItem.Click += (s, e) => SaveFileAs();
            fileButton.DropDownItems.Add(saveAsMenuItem);

            fileButton.DropDownItems.Add(new ToolStripSeparator());

            recentFilesMenu = new ToolStripMenuItem("Open Recent");
            fileButton.DropDownItems.Add(recentFilesMenu);

            // 添加文件按钮到工具栏
            toolStrip.Items.Add(fileButton);

            var viewButton = new ToolStripDropDownButton("View");

            var zoomFitItem = new ToolStripMenuItem("Zoom to Fit All");
            zoomFitItem.ShortcutKeys = Keys.Control | Keys.D0;
            zoomFitItem.Click += (s, e) => nodeEditor.ZoomToFitAll();
            viewButton.DropDownItems.Add(zoomFitItem);

            var zoomSelectionItem = new ToolStripMenuItem("Zoom to Selection");
            zoomSelectionItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.D0;
            zoomSelectionItem.Click += (s, e) => nodeEditor.ZoomToSelection();
            viewButton.DropDownItems.Add(zoomSelectionItem);

            toolStrip.Items.Add(viewButton);

            var editButton = new ToolStripDropDownButton("Edit");

            var undoItem = new ToolStripMenuItem("Undo");
            undoItem.ShortcutKeys = Keys.Control | Keys.Z;
            undoItem.Click += (s, e) => nodeEditor.Undo();
            editButton.DropDownItems.Add(undoItem);

            var redoItem = new ToolStripMenuItem("Redo");
            redoItem.ShortcutKeys = Keys.Control | Keys.Y;
            redoItem.Click += (s, e) => nodeEditor.Redo();
            editButton.DropDownItems.Add(redoItem);

            editButton.DropDownItems.Add(new ToolStripSeparator());

            var alignLeftItem = new ToolStripMenuItem("Align Left");
            alignLeftItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.L;
            alignLeftItem.Click += (s, e) => nodeEditor.AlignSelection(NodeAlignEdge.Left);
            editButton.DropDownItems.Add(alignLeftItem);

            var alignRightItem = new ToolStripMenuItem("Align Right");
            alignRightItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.R;
            alignRightItem.Click += (s, e) => nodeEditor.AlignSelection(NodeAlignEdge.Right);
            editButton.DropDownItems.Add(alignRightItem);

            var alignTopItem = new ToolStripMenuItem("Align Top");
            alignTopItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.T;
            alignTopItem.Click += (s, e) => nodeEditor.AlignSelection(NodeAlignEdge.Top);
            editButton.DropDownItems.Add(alignTopItem);

            var alignBottomItem = new ToolStripMenuItem("Align Bottom");
            alignBottomItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.B;
            alignBottomItem.Click += (s, e) => nodeEditor.AlignSelection(NodeAlignEdge.Bottom);
            editButton.DropDownItems.Add(alignBottomItem);

            editButton.DropDownItems.Add(new ToolStripSeparator());

            var distributeHItem = new ToolStripMenuItem("Distribute Horizontally");
            distributeHItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.H;
            distributeHItem.Click += (s, e) => nodeEditor.DistributeSelectionHorizontally();
            editButton.DropDownItems.Add(distributeHItem);

            var distributeVItem = new ToolStripMenuItem("Distribute Vertically");
            distributeVItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.J;
            distributeVItem.Click += (s, e) => nodeEditor.DistributeSelectionVertically();
            editButton.DropDownItems.Add(distributeVItem);

            editButton.DropDownItems.Add(new ToolStripSeparator());

            var deleteItem = new ToolStripMenuItem("Delete");
            deleteItem.ShortcutKeyDisplayString = "Del";
            deleteItem.Click += (s, e) => nodeEditor.DeleteSelection();
            editButton.DropDownItems.Add(deleteItem);

            toolStrip.Items.Add(editButton);

            var helpButton = new ToolStripDropDownButton("Help");
            var shortcutsItem = new ToolStripMenuItem("Keyboard Shortcuts...");
            shortcutsItem.Click += (s, e) => ShowKeyboardShortcutsHelp();
            helpButton.DropDownItems.Add(shortcutsItem);
            toolStrip.Items.Add(helpButton);

            // 添加分隔符
            toolStrip.Items.Add(new ToolStripSeparator());

            // 添加执行控制按钮：运行/继续、单步、停止
            var executeButton = new ToolStripButton("Run/Continue");
            executeButton.Click += (s, e) => nodeEditor.ExecuteFlow();
            toolStrip.Items.Add(executeButton);

            var stepButton = new ToolStripButton("Step");
            stepButton.Click += (s, e) => nodeEditor.StepExecution();
            toolStrip.Items.Add(stepButton);

            var stopButton = new ToolStripButton("Stop");
            stopButton.Click += (s, e) => nodeEditor.StopExecution();
            toolStrip.Items.Add(stopButton);

            this.Controls.Add(toolStrip);
        }

        private static void ShowKeyboardShortcutsHelp()
        {
            const string text =
                "画布编辑\r\n" +
                "  右键空白          搜索并创建节点\r\n" +
                "  右键连线          删除连线\r\n" +
                "  中键拖拽          平移画布\r\n" +
                "  滚轮              缩放\r\n" +
                "  Ctrl+A            全选\r\n" +
                "  Ctrl+C / Ctrl+V   复制 / 粘贴\r\n" +
                "  Delete            删除选中节点\r\n" +
                "  F9                切换断点（单选）\r\n" +
                "\r\n" +
                "Edit 菜单\r\n" +
                "  Ctrl+Z / Ctrl+Y   撤销 / 重做\r\n" +
                "  Ctrl+Shift+L/R/T/B  左/右/上/下对齐\r\n" +
                "  Ctrl+Shift+H/J    水平 / 垂直等距（≥3 节点）\r\n" +
                "\r\n" +
                "View 菜单\r\n" +
                "  Ctrl+0            适应全部节点\r\n" +
                "  Ctrl+Shift+0      缩放至选中";

            MessageBox.Show(text, "快捷键", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void InitializeNodeTreeView()
        {
            nodeTreeView = new TreeView
            {
                Dock = DockStyle.Left,
                Width = 200,
                Height = 200,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White
            };

            var nodePaths = NodeFactory.GetNodePath();
            var rootNodes = new Dictionary<string, TreeNode>();

            foreach (var nodePath in nodePaths)
            {
                // nodeTreeView.Nodes.Add(new TreeNode(nodePath));
                var pathParts = nodePath.Split('/').Where(p => !string.IsNullOrEmpty(p)).ToArray();
                TreeNode currentParent = null;
                var currentPath = "";

                // 遍历路径的每一部分，构建树形结构
                for (int i = 0; i < pathParts.Length; i++)
                {
                    var part = pathParts[i];
                    currentPath = currentPath == "" ? part : currentPath + "/" + part;

                    if (!rootNodes.ContainsKey(currentPath))
                    {
                        var newNode = new TreeNode(part) { Tag = i == pathParts.Length - 1 ? nodePath : null };
                        rootNodes[currentPath] = newNode;

                        if (currentParent == null)
                            nodeTreeView.Nodes.Add(newNode);
                        else
                            currentParent.Nodes.Add(newNode);
                    }

                    currentParent = rootNodes[currentPath];
                }
            }



            // TreeView 只需要 ItemDrag 事件
            nodeTreeView.ItemDrag += NodeTreeView_ItemDrag;
            //  Controls.Add(nodeTreeView);

            flowLayoutPanel1.Controls.Add(nodeTreeView);
        }

        private void InitializeVariableListControl()
        {
            variableListControl = new VariableListControl
            {
                Dock = DockStyle.Left,
                Width = 200,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White
                // Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            // Position the control in the bottom-left corner
            //  variableListControl.Location = new Point(10, this.ClientSize.Height - variableListControl.Height - 10);

            // this.Controls.Add(variableListControl);


            flowLayoutPanel1.Controls.Add(variableListControl);



        }

        private void InitializeNodeEditor()
        {
            nodeEditor = new NodeEditor
            {
                Dock = DockStyle.Fill, // 填充剩余空间
                AllowDrop = true
            };

            nodeEditor.DragEnter += NodeEditor_DragEnter;
            nodeEditor.DragDrop += NodeEditor_DragDrop;
            Controls.Add(nodeEditor);
        }

        private void InitializePropertyPanel()
        {
            propertyPanel = new PropertyPanel(nodeEditor)
            {
                Dock = DockStyle.Right,
                Width = 250,
                BackColor = Color.FromArgb(30, 30, 30)
            };
            Controls.Add(propertyPanel);

            // 让填充画布的 nodeEditor 处于 z-order 最底层，这样左/右/顶部停靠控件先占边、画布填充剩余区域
            nodeEditor.SendToBack();

            // 选中节点变化时刷新属性面板
            nodeEditor.SelectionChanged += RefreshPropertyPanel;
            nodeEditor.GraphChanged += RefreshPropertyPanel;
        }

        private void RefreshPropertyPanel()
        {
            var selected = nodeEditor.SelectedNodes;
            if (selected.Count == 1)
            {
                propertyPanel.ShowProperties(selected.First().Node);
            }
            else
            {
                propertyPanel.ClearProperties();
            }
        }

        private void NewFile()
        {
            if (ConfirmDiscardChanges() != DialogResult.Yes)
            {
                return;
            }

            nodeEditor.NewGraph();
            currentFilePath = null;
            dataView?.RefreshList();
            propertyPanel?.ClearProperties();
            CaptureCleanFingerprint();
            ClearDirty();
        }

        private void OpenFile()
        {
            if (ConfirmDiscardChanges() != DialogResult.Yes)
            {
                return;
            }

            using (OpenFileDialog openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
                openDialog.FilterIndex = 1;
                openDialog.RestoreDirectory = true;

                if (openDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                OpenFilePath(openDialog.FileName);
            }
        }

        private void OpenFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                MessageBox.Show("文件不存在或路径无效。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                recentFiles.Remove(filePath);
                SaveRecentFiles();
                RefreshRecentMenu();
                return;
            }

            try
            {
                nodeEditor.NewGraph();
                nodeEditor.LoadFromFile(filePath);
                currentFilePath = filePath;
                dataView?.RefreshList();
                propertyPanel?.ClearProperties();
                CaptureCleanFingerprint();
                ClearDirty();
                AddRecentFile(filePath);
                UpdateTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddRecentFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            recentFiles.Remove(filePath);
            recentFiles.Insert(0, filePath);
            while (recentFiles.Count > MaxRecentFiles)
            {
                recentFiles.RemoveAt(recentFiles.Count - 1);
            }

            SaveRecentFiles();
            RefreshRecentMenu();
        }

        private void LoadRecentFiles()
        {
            recentFiles.Clear();
            try
            {
                if (!File.Exists(RecentFilesStorePath))
                    return;

                foreach (var line in File.ReadAllLines(RecentFilesStorePath))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    var path = line.Trim();
                    if (File.Exists(path))
                        recentFiles.Add(path);
                }
            }
            catch
            {
                // 忽略损坏的 recent 文件
            }
        }

        private void SaveRecentFiles()
        {
            try
            {
                var dir = Path.GetDirectoryName(RecentFilesStorePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllLines(RecentFilesStorePath, recentFiles);
            }
            catch
            {
                // 非关键路径，忽略写入失败
            }
        }

        private void RefreshRecentMenu()
        {
            if (recentFilesMenu == null)
                return;

            recentFilesMenu.DropDownItems.Clear();
            if (recentFiles.Count == 0)
            {
                recentFilesMenu.DropDownItems.Add(new ToolStripMenuItem("(无)") { Enabled = false });
                return;
            }

            foreach (var path in recentFiles)
            {
                var item = new ToolStripMenuItem(Path.GetFileName(path))
                {
                    ToolTipText = path
                };
                var captured = path;
                item.Click += (s, e) =>
                {
                    if (ConfirmDiscardChanges() != DialogResult.Yes)
                        return;
                    OpenFilePath(captured);
                };
                recentFilesMenu.DropDownItems.Add(item);
            }
        }

        private void SaveFile()
        {
            // 已有文件路径则直接保存，否则走另存为
            if (string.IsNullOrEmpty(currentFilePath))
            {
                SaveFileAs();
                return;
            }

            try
            {
                nodeEditor.SaveToFile(currentFilePath);
                CaptureCleanFingerprint();
                ClearDirty();
                AddRecentFile(currentFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveFileAs()
        {
            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
                saveDialog.FilterIndex = 1;
                saveDialog.RestoreDirectory = true;
                if (!string.IsNullOrEmpty(currentFilePath))
                {
                    saveDialog.FileName = System.IO.Path.GetFileName(currentFilePath);
                }

                if (saveDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    nodeEditor.SaveToFile(saveDialog.FileName);
                    currentFilePath = saveDialog.FileName;
                    CaptureCleanFingerprint();
                    ClearDirty();
                    AddRecentFile(currentFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void UpdateTitle()
        {
            var fileName = string.IsNullOrEmpty(currentFilePath)
                ? "Untitled"
                : System.IO.Path.GetFileName(currentFilePath);
            var dirtyMark = isDirty ? " *" : "";
            this.Text = $"FlowNode - {fileName}{dirtyMark}";
        }

        private void InitializeDataView()
        {
            dataView = new DataViewControl(
                nodeEditor.NodeManager,
                nodeEditor.CommandManager,
                () => nodeEditor.Invalidate());
            flowLayoutPanel1.Controls.Add(dataView);

            // Add drag & drop event handlers
            dataView.listView.ItemDrag += ListView_ItemDrag;
            //dataView.listView.DragEnter += ListView_DragEnter;
            //dataView.listView.DragDrop += ListView_DragDrop;

        }



        private void NodeTreeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (e.Item is TreeNode item && item.Tag != null)
            {
                DoDragDrop(item, DragDropEffects.Copy);
            }
        }

        private void ListView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (e.Item is ListViewItem item)
            {
                DoDragDrop(item, DragDropEffects.Copy);
            }
        }

        private void NodeEditor_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(TreeNode)) || e.Data.GetDataPresent(typeof(ListViewItem)))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void NodeEditor_DragDrop(object sender, DragEventArgs e)
        {
            var clientPoint = nodeEditor.PointToClient(new Point(e.X, e.Y));
            var location = nodeEditor.ScreenToNode(clientPoint);
            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                var treeNodeItem = (TreeNode)e.Data.GetData(typeof(TreeNode));
                var nodePath = (string)treeNodeItem.Tag;
                try
                {
                    var node = NodeFactory.CreateNode(nodePath);
                    nodeEditor.AddNode(node, location);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error creating node: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (e.Data.GetDataPresent(typeof(ListViewItem)))
            {
                var listViewItem = (ListViewItem)e.Data.GetData(typeof(ListViewItem));
                try
                {
                        DialogResult result = MessageBox.Show(
                        "Do you want to create a set variable node?\nYes = Write Node, No = Read Node",
                        "Variable Node Type",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    
                    bool isSet = (result == DialogResult.Yes);
                    var node = NodeFactory.CreateVarNode(listViewItem.Text, nodeEditor.NodeManager.GetDataObjectType(listViewItem.Text), isSet);
                    nodeEditor.AddNode(node, location);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error creating node: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }




    }
}
