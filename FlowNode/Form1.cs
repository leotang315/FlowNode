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
    public partial class Form1 : Form
    {

        private List<Node> nodes;
        private List<NodeView> nodeViews; // 存储视图数据
        private List<Connection> connections; // 存储连接
        private Node selectedNode;
        private Point mouseOffset;
        private bool isDragging;


        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true; // 启用双缓冲以减少闪烁
            nodes = new List<Node>();
            nodeViews = new List<NodeView>(); // 初始化视图数据列表
            connections = new List<Connection>(); // 初始化连接列表
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DrawNodes(e.Graphics); // 绘制节点

            DrawConnections(e.Graphics); // 绘制连接线
                  }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            // 检查是否点击到现有节点
            bool test = false;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Contains(e.Location))
                {
                    test = true;
                    if (selectedNode == null || selectedNode == nodes[i])
                    {
                        selectedNode = nodes[i];
                        mouseOffset = new Point(e.X - selectedNode.X, e.Y - selectedNode.Y);
                        isDragging = true;
                    }
                    else
                    {
                        // 如果已经选择一个节点，创建连接
                        connections.Add(new Connection(selectedNode, nodes[i]));
                    }
                }
            }

            if (!test)
            {
                selectedNode = null;
            }
            Invalidate(); // 重新绘制

        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && selectedNode != null)
            {
                // 更新节点位置
                selectedNode.X = e.X - mouseOffset.X;
                selectedNode.Y = e.Y - mouseOffset.Y;

                // 更新窗口
                Invalidate(); // 重新绘制
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
            Invalidate(); // 重新绘制
        }

        private void DrawNodes(Graphics g)
        {
            foreach (var nodeView in nodeViews)
            {
                if (nodeView.GetNode() == selectedNode)
                {
                    nodeView.Draw(g, true); // 选中节点要高亮

                }
                else
                {
                    nodeView.Draw(g, false); // 使用视图类绘制节点

                }
            }
        }

        private void DrawConnections(Graphics g)
        {
            using (Pen pen = new Pen(Color.Black, 2))
            {
                foreach (var connection in connections)
                {
                    // g.DrawLine(pen, connection.NodeA.Center, connection.NodeB.Center);
                    float x1 = connection.NodeA.Center.X;
                    float y1 = connection.NodeA.Center.Y;
                    float x2 = connection.NodeB.Center.X;
                    float y2 = connection.NodeB.Center.Y;
                    float n = 100;
                    g.DrawBezier(pen, x1, y1, x1 + n, y1, x2 - n, y2, x2, y2);
                }
            }
        }

        private void Form1_DoubleClick(object sender, EventArgs e)
        {
            // 添加新节点
            Node newNode = new Node(0, 0);
            nodes.Add(newNode);
            nodeViews.Add(new NodeView(newNode)); // 添加视图数据
            Invalidate(); // 重新绘制

        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Z))
            {
                nodeEditor.Undo();
                return true;
            }
            else if (keyData == (Keys.Control | Keys.Y))
            {
                nodeEditor.Redo();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    public class NodeView
    {
        private Node _node;

        public NodeView(Node node)
        {
            _node = node;
        }

        public Node GetNode()
        {
            return _node;
        }
        public void Draw(Graphics g, bool isSelected)
        {

            g.FillRectangle(Brushes.LightBlue, _node.X, _node.Y, _node.Width, _node.Height);
            if (isSelected)
            {
                g.DrawRectangle(Pens.Orange, _node.X, _node.Y, _node.Width, _node.Height);
            }
            else
            {
                g.DrawRectangle(Pens.Black, _node.X, _node.Y, _node.Width, _node.Height);
            }

        }

        public Point GetCenter()
        {
            return new Point(_node.X + _node.Width / 2, _node.Y + _node.Height / 2);
        }
    }

    public class Node
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; } = 100;
        public int Height { get; } = 50;

        public Node(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Contains(Point point)
        {
            return point.X >= X && point.X <= X + Width &&
                   point.Y >= Y && point.Y <= Y + Height;
        }
        public Point Center => new Point(X + Width / 2, Y + Height / 2);
    }

    public class Connection
    {
        public Node NodeA { get; }
        public Node NodeB { get; }

        public Connection(Node nodeA, Node nodeB)
        {
            NodeA = nodeA;
            NodeB = nodeB;
        }
    }
}
