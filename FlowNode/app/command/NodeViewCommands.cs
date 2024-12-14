using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using FlowNode.node;
using FlowNode.app.view;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
namespace FlowNode.app.command
{
    /// <summary>
    /// 节点视图操作的命令
    /// </summary>
    public class AddNodeViewCommand : ICommand
    {
        private Dictionary<INode, NodeView> nodeViews;
        private Point location;
        private NodeView nodeView;

        public AddNodeViewCommand(Dictionary<INode, NodeView> nodeViews, NodeView nodeView , Point location)
        {
            this.nodeViews = nodeViews;
            this.location = location;
            this.nodeView = nodeView;
        }

        public void Execute()
        {
            nodeViews[nodeView.Node] = nodeView;
        }

        public void Undo()
        {
            nodeViews.Remove(nodeView.Node);
        }
    }

    public class RemoveNodeViewCommand : ICommand
    {
        private readonly Dictionary<INode, NodeView> nodeViews;
        private readonly INode node;
        private NodeView nodeView;

        public RemoveNodeViewCommand(Dictionary<INode, NodeView> nodeViews, INode node)
        {
            this.nodeViews = nodeViews;
            this.node = node;
        }

        public void Execute()
        {
            nodeView = nodeViews[node];
            nodeViews.Remove(node);
        }

        public void Undo()
        {
            nodeViews.Add(node, nodeView);
        }
    }

    public class MoveNodeViewCommand : ICommand
    {
        private readonly NodeView nodeView;
        private readonly Point oldLocation;
        private readonly Point newLocation;

        public MoveNodeViewCommand(NodeView nodeView, Point oldLocation, Point newLocation)
        {
            this.nodeView = nodeView;
            this.oldLocation = oldLocation;
            this.newLocation = newLocation;
        }

        public void Execute()
        {
            nodeView.Bounds = new Rectangle(newLocation, nodeView.Bounds.Size);
        }

        public void Undo()
        {
            nodeView.Bounds = new Rectangle(oldLocation, nodeView.Bounds.Size);
        }
    }
} 