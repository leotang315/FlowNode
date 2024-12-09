using FlowNode.node;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowNode.app.command
{
    // 示例命令类
    public class AddNodeCommand : ICommand
    {
        private NodeEditor editor;
        private NodeBase node;
        private Point position;

        public AddNodeCommand(NodeEditor editor, NodeBase node, Point position)
        {
            this.editor = editor;
            this.node = node;
            this.position = position;
        }

        public void Execute()
        {
            editor.AddNode(node, position);
        }

        public void Undo()
        {
            editor.RemoveNode(node);
        }
    }
}
