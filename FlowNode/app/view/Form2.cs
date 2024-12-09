using FlowNode;
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
    public partial class Form2 : Form
    {
        private NodeEditor nodeEditor;
        private TreeView nodeTreeView;
        public Form2()
        {
            InitializeComponent();


            ToolStrip toolStrip = new ToolStrip();
            toolStrip.Items.Add("Execute").Click += (s, e) =>
            {
                nodeEditor.ExecuteFlow();
            };
            this.Controls.Add(toolStrip);
            InitializeNodeTreeView();

            InitializeNodeEditor();           
        }

        private void InitializeNodeTreeView()
        {
            nodeTreeView = new TreeView
            {
                Dock = DockStyle.Left,
                Width = 200,
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

            nodeTreeView.AfterSelect += NodeTreeView_AfterSelect;

            // TreeView 只需要 ItemDrag 事件
            nodeTreeView.ItemDrag += NodeTreeView_ItemDrag;
            Controls.Add(nodeTreeView);
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


        private void NodeTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {

            var nodePath = e.Node.Tag as string;
            if (nodePath != null)
            {
                try
                {
                    var node = NodeFactory.CreateNode(nodePath);
                    nodeEditor.AddNode(node, new Point(100, 100));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error creating node: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void NodeTreeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (e.Item is TreeNode node && node.Tag != null)
            {
                DoDragDrop(node.Tag, DragDropEffects.Copy);
            }
        }

        private void NodeEditor_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(string)))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void NodeEditor_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(string)))
            {
                var nodePath = (string)e.Data.GetData(typeof(string));
                var clientPoint = nodeTreeView.PointToClient(new Point(e.X, e.Y));
                var location = nodeEditor.ScreenToNode(clientPoint);

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
        }
        // 示例节点类
        public class MathNode : NodeBase
        {
            public override void allocateDefaultPins()
            {
                createPin("A", PinDirection.Input, PinType.Data, typeof(float), 0f);
                createPin("B", PinDirection.Input, PinType.Data, typeof(float), 0f);
                createPin("Result", PinDirection.Output, PinType.Data, typeof(float), 0f);
                createPin("Exec In", PinDirection.Input, PinType.Execute);
                createPin("Exec Out", PinDirection.Output, PinType.Execute);
            }

            public override void excute(INodeManager manager)
            {
                var a = (float)findPin("A").data;
                var b = (float)findPin("B").data;
                findPin("Result").data = a + b;
                manager.pushNextConnectNode(findPin("Exec Out"));
            }
        }
    }
}
