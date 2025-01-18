using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

using System.Linq;
using FlowNode.node;
using FlowNode.app.view;
using FlowNode.app.command;
namespace FlowNode
{
    public class DraggingNodeState : EditorState
    {
        private readonly NodeView nodeView;
        private readonly Point dragStart;
        private readonly Point nodeStart;
        private readonly Dictionary<NodeView, Point> selectedNodesStartPos;
        public override string getName()
        {
            return "DraggingNodeState";
        }
        public DraggingNodeState(NodeEditor editor, NodeView nodeView, Point dragStart) : base(editor)
        {
            this.nodeView = nodeView;
            this.dragStart = dragStart;
            this.nodeStart = nodeView.Bounds.Location;
            this.selectedNodesStartPos = editor.SelectedNodes
                .ToDictionary(n => n, n => n.Bounds.Location);
        }

        public override void OnMouseMove(MouseEventArgs e)
        {
            var mousePos = ScreenToNode(e.Location);
            var dx = mousePos.X - dragStart.X;
            var dy = mousePos.Y - dragStart.Y;

            foreach (var pair in selectedNodesStartPos)
            {
                var startPos = pair.Value;
                pair.Key.Bounds = new Rectangle(
                    startPos.X + dx,
                    startPos.Y + dy,
                    pair.Key.Bounds.Width,
                    pair.Key.Bounds.Height
                );
            }
            Editor.Invalidate();
        }

        public override void OnMouseUp(MouseEventArgs e)
        {
            if (nodeView.Bounds.Location != nodeStart)
            {
                var command = new CompositeCommand();
                foreach (var pair in selectedNodesStartPos)
                {
                    command.AddCommand(new MoveNodeViewCommand(
                        pair.Key,
                        pair.Value,
                        pair.Key.Bounds.Location));
                }
                Editor.CommandManager.ExecuteCommand(command);
            }
            Editor.ChangeState(new IdleState(Editor));
        }
    }
}