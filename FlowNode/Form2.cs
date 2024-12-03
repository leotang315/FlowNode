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
        public Form2()
        {
            InitializeComponent();
            nodeEditor = new NodeEditor();
            nodeEditor.Dock = DockStyle.Fill;
            this.Controls.Add(nodeEditor);

            // 添加工具栏
            ToolStrip toolStrip = new ToolStrip();

            // 示例：添加一个数学节点
            toolStrip.Items.Add("Add Node").Click += (s, e) =>
            {
                var mathNode = new MathNode();
                mathNode.Name = "Math";
                nodeEditor.AddNode(mathNode, new Point(50, 50));
            };

            toolStrip.Items.Add("Execute").Click += (s, e) =>
                nodeEditor.ExecuteFlow();

            this.Controls.Add(toolStrip);
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
