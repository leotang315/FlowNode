using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FlowNode.node;

namespace FlowNode
{
    /// <summary>
    /// 右键空白处弹出的节点搜索菜单：按路径分组展示 NodeFactory 中已注册节点，
    /// 支持关键字过滤，选中后在指定画布位置创建节点。
    /// </summary>
    public class NodeSearchMenu : Form
    {
        private TextBox searchBox;
        private TreeView resultTree;

        private readonly NodeEditor editor;
        private readonly Point nodeLocation;
        private List<string> allNodePaths;

        public NodeSearchMenu(NodeEditor editor, Point screenLocation, Point nodeLocation)
        {
            this.editor = editor;
            this.nodeLocation = nodeLocation;

            InitializeComponents(screenLocation);
            InitializeNodeTypes();
            RefreshResults();
        }

        private void InitializeComponents(Point screenLocation)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Location = screenLocation;
            Size = new Size(280, 360);
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.FromArgb(60, 60, 60);

            searchBox = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 25,
                BorderStyle = BorderStyle.FixedSingle
            };
            searchBox.TextChanged += (s, e) => RefreshResults();
            searchBox.KeyDown += SearchBox_KeyDown;

            resultTree = new TreeView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                HideSelection = false,
                FullRowSelect = true,
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = false,
                Indent = 16
            };
            resultTree.DoubleClick += (s, e) => CreateSelectedNode();
            resultTree.KeyDown += ResultTree_KeyDown;

            Controls.Add(resultTree);
            Controls.Add(searchBox);

            Shown += (s, e) => searchBox.Focus();
        }

        private void InitializeNodeTypes()
        {
            allNodePaths = NodeFactory.GetNodePath()
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void RefreshResults()
        {
            string keyword = searchBox.Text.Trim().ToLowerInvariant();

            var filtered = allNodePaths.Where(path =>
                keyword.Length == 0 || path.ToLowerInvariant().Contains(keyword));

            resultTree.BeginUpdate();
            resultTree.Nodes.Clear();
            BuildPathTree(resultTree.Nodes, filtered);
            ExpandFirstLevel();
            SelectFirstLeaf();
            resultTree.EndUpdate();
        }

        private static void BuildPathTree(TreeNodeCollection parentNodes, IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                var segments = path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                var current = parentNodes;

                for (int i = 0; i < segments.Length; i++)
                {
                    string segment = segments[i];
                    bool isLeaf = i == segments.Length - 1;

                    TreeNode node = FindChild(current, segment);
                    if (node == null)
                    {
                        node = new TreeNode(segment);
                        current.Add(node);
                    }

                    if (isLeaf)
                    {
                        node.Tag = path;
                    }

                    current = node.Nodes;
                }
            }
        }

        private static TreeNode FindChild(TreeNodeCollection nodes, string text)
        {
            foreach (TreeNode node in nodes)
            {
                if (string.Equals(node.Text, text, StringComparison.OrdinalIgnoreCase))
                    return node;
            }
            return null;
        }

        private void ExpandFirstLevel()
        {
            foreach (TreeNode node in resultTree.Nodes)
            {
                node.Expand();
            }
        }

        private void SelectFirstLeaf()
        {
            var leaf = FindFirstLeaf(resultTree.Nodes);
            if (leaf != null)
            {
                resultTree.SelectedNode = leaf;
            }
        }

        private static TreeNode FindFirstLeaf(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Tag is string)
                    return node;
                var nested = FindFirstLeaf(node.Nodes);
                if (nested != null)
                    return nested;
            }
            return null;
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                CreateSelectedNode();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
            else if (e.KeyCode == Keys.Down && resultTree.Nodes.Count > 0)
            {
                resultTree.Focus();
                e.Handled = true;
            }
        }

        private void ResultTree_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                CreateSelectedNode();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        }

        private void CreateSelectedNode()
        {
            var selected = resultTree.SelectedNode;
            if (selected?.Tag is string path)
            {
                try
                {
                    var node = NodeFactory.CreateNode(path);
                    editor.AddNode(node, nodeLocation);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"创建节点失败：{path}\n{ex.Message}", "FlowNode",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                finally
                {
                    Close();
                }
            }
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Close();
        }
    }
}
