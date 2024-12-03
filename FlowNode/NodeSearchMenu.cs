using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using FlowNode.node;
namespace FlowNode
{
    public class NodeSearchMenu : Form
    {
        private TextBox searchBox;
        private ListBox resultList;
        private Dictionary<string, Type> nodeTypes;
        private Point createPosition;

        public NodeSearchMenu(Point position, NodeEditor editor)
        {
            createPosition = position;
            InitializeComponents();
            InitializeNodeTypes();
            
            // 当选择节点类型时创建节点
            resultList.SelectedIndexChanged += (s, e) =>
            {
                if (resultList.SelectedItem != null)
                {
                    string selectedName = resultList.SelectedItem.ToString();
                    if (nodeTypes.TryGetValue(selectedName, out Type nodeType))
                    {
                        var node = (NodeBase)Activator.CreateInstance(nodeType);
                        editor.AddNode(node, createPosition);
                        Close();
                    }
                }
            };
        }

        private void InitializeComponents()
        {
            // 设置窗体样式
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = createPosition;
            this.Size = new Size(200, 300);

            // 创建搜索框
            searchBox = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 25
            };
            searchBox.TextChanged += SearchBox_TextChanged;

            // 创建结果列表
            resultList = new ListBox
            {
                Dock = DockStyle.Fill
            };

            this.Controls.Add(resultList);
            this.Controls.Add(searchBox);
        }

        private void InitializeNodeTypes()
        {
            nodeTypes = new Dictionary<string, Type>();
            // 在这里注册所有可用的节点类型
            // 例如：nodeTypes.Add("Print", typeof(PrintNode));
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            resultList.Items.Clear();
            string search = searchBox.Text.ToLower();
            
            foreach (var nodeName in nodeTypes.Keys)
            {
                if (nodeName.ToLower().Contains(search))
                {
                    resultList.Items.Add(nodeName);
                }
            }
        }
    }
} 