using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FlowNode.node;

namespace FlowNode
{
    /// <summary>
    /// 右键空白处弹出的节点搜索菜单：列出 NodeFactory 中已注册的所有节点路径，
    /// 支持关键字过滤，选中后在指定画布位置创建节点。
    /// </summary>
    public class NodeSearchMenu : Form
    {
        private TextBox searchBox;
        private ListBox resultList;

        private readonly NodeEditor editor;
        private readonly Point nodeLocation;   // 画布坐标，用于创建节点
        private List<string> allNodePaths;

        /// <param name="editor">目标编辑器</param>
        /// <param name="screenLocation">菜单显示的屏幕坐标</param>
        /// <param name="nodeLocation">节点创建的画布坐标</param>
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
            Size = new Size(240, 320);
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

            resultList = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            resultList.DoubleClick += (s, e) => CreateSelectedNode();
            resultList.KeyDown += ResultList_KeyDown;

            Controls.Add(resultList);
            Controls.Add(searchBox);

            // 让搜索框拿到焦点，弹出即可直接输入
            Shown += (s, e) => searchBox.Focus();
        }

        private void InitializeNodeTypes()
        {
            // 直接复用 NodeFactory 反射得到的所有已注册节点路径
            allNodePaths = NodeFactory.GetNodePath()
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void RefreshResults()
        {
            string keyword = searchBox.Text.Trim().ToLowerInvariant();

            resultList.BeginUpdate();
            resultList.Items.Clear();
            foreach (var path in allNodePaths)
            {
                if (keyword.Length == 0 || path.ToLowerInvariant().Contains(keyword))
                {
                    resultList.Items.Add(path);
                }
            }
            if (resultList.Items.Count > 0)
            {
                resultList.SelectedIndex = 0;
            }
            resultList.EndUpdate();
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
            else if (e.KeyCode == Keys.Down && resultList.Items.Count > 0)
            {
                // 方向键下移焦点到列表，便于继续选择
                resultList.Focus();
                e.Handled = true;
            }
        }

        private void ResultList_KeyDown(object sender, KeyEventArgs e)
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
            if (resultList.SelectedItem == null)
            {
                return;
            }

            string path = resultList.SelectedItem.ToString();
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

        protected override void OnDeactivate(EventArgs e)
        {
            // 点击菜单外区域即关闭，表现为弹出菜单
            base.OnDeactivate(e);
            Close();
        }
    }
}
