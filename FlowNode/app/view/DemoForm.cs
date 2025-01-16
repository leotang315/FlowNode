using FlowNode;
using FlowNode.app.serialization;
using FlowNode.app.view;
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
    public partial class DemoForm : Form
    {

        private NodeEditor nodeEditor;
        private TreeView nodeTreeView;
        private VariableListControl variableListControl;
        public DemoForm()
        {
            InitializeComponent();



            InitializeMenu();
            InitializeNodeEditor();
            InitializeNodeTreeView();
            //InitializeVariableListControl();

            InitializeDataView();
        }

        private void InitializeMenu()
        {
            // 创建主工具栏
            ToolStrip toolStrip = new ToolStrip();

            // 创建文件下拉按钮
            var fileButton = new ToolStripDropDownButton("File");

            // 创建保存菜单项
            var saveMenuItem = new ToolStripMenuItem("Save");
            saveMenuItem.Click += (s, e) =>
            {
                using (SaveFileDialog saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
                    saveDialog.FilterIndex = 1;
                    saveDialog.RestoreDirectory = true;

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            nodeEditor.SaveToFile(saveDialog.FileName);
                            MessageBox.Show("保存成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            };
            fileButton.DropDownItems.Add(saveMenuItem);

            // 创建打开菜单项
            var openMenuItem = new ToolStripMenuItem("Open");
            openMenuItem.Click += (s, e) =>
            {
                using (OpenFileDialog openDialog = new OpenFileDialog())
                {
                    openDialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
                    openDialog.FilterIndex = 1;
                    openDialog.RestoreDirectory = true;

                    if (openDialog.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            nodeEditor.LoadFromFile(openDialog.FileName);
                            MessageBox.Show("加载成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"加载失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            };
            fileButton.DropDownItems.Add(openMenuItem);

            // 添加文件按钮到工具栏
            toolStrip.Items.Add(fileButton);

            // 添加分隔符
            toolStrip.Items.Add(new ToolStripSeparator());

            // 添加执行按钮
            var executeButton = new ToolStripButton("Execute");
            executeButton.Click += (s, e) => nodeEditor.ExecuteFlow();
            toolStrip.Items.Add(executeButton);

            this.Controls.Add(toolStrip);
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

        private void InitializeDataView()
        {
            DataViewControl dataView = new DataViewControl(nodeEditor.NodeManager, nodeEditor.CommandManager);
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
