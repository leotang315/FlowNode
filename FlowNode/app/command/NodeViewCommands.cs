using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using FlowNode.node;
namespace FlowNode.app.command
{
    /// <summary>
    /// 节点视图操作的命令
    /// </summary>
    public class AddNodeViewCommand : ICommand
    {
        private readonly Dictionary<INode, NodeView> nodeViews;
        private readonly INode node;
        private readonly Point location;
        private NodeView nodeView;

        public AddNodeViewCommand(Dictionary<INode, NodeView> nodeViews, INode node, Point location)
        {
            this.nodeViews = nodeViews;
            this.node = node;
            this.location = location;
        }

        public void Execute()
        {
            nodeView = new NodeView((NodeBase)node, location);
            nodeViews.Add(node, nodeView);
        }

        public void Undo()
        {
            nodeViews.Remove(node);
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
            nodeView.UpdatePinLocations();
        }

        public void Undo()
        {
            nodeView.Bounds = new Rectangle(oldLocation, nodeView.Bounds.Size);
            nodeView.UpdatePinLocations();
        }
    }
} 