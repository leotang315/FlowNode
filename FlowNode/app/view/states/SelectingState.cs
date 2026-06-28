using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using FlowNode.app.view;

namespace FlowNode
{
    public class SelectingState : EditorState
    {
        private readonly Point startPoint;
        private Point currentPoint;
        private RectangleF lastMarqueeDirty;
        private HashSet<NodeView> previousSelected = new HashSet<NodeView>();

        public override string getName()
        {
            return "SelectingState";
        }

        public SelectingState(NodeEditor editor, Point start) : base(editor)
        {
            startPoint = start;
            currentPoint = start;
            previousSelected = new HashSet<NodeView>(Editor.SelectedNodes);
        }

        public override void OnMouseMove(MouseEventArgs e)
        {
            currentPoint = ScreenToNode(e.Location);
            var marquee = GetSelectionRect();
            var marqueeDirty = RectangleF.FromLTRB(marquee.Left, marquee.Top, marquee.Right, marquee.Bottom);
            marqueeDirty.Inflate(2, 2);

            var newSelected = new HashSet<NodeView>();
            foreach (var nodeView in Editor.NodeViews.Values)
            {
                if (marquee.IntersectsWith(nodeView.Bounds))
                    newSelected.Add(nodeView);
            }

            var dirty = EditorViewport.Union(marqueeDirty, lastMarqueeDirty);
            foreach (var view in previousSelected.Union(newSelected))
                dirty = EditorViewport.Union(dirty, EditorViewport.ExpandNodeBounds(view.Bounds));

            Editor.ClearSelection();
            foreach (var nodeView in newSelected)
                Editor.AddToSelection(nodeView);

            Editor.InvalidateWorldRect(dirty);
            previousSelected = newSelected;
            lastMarqueeDirty = marqueeDirty;
        }

        public override void OnMouseUp(MouseEventArgs e)
        {
            Editor.ChangeState(new IdleState(Editor));
        }

        public override void OnPaint(Graphics g)
        {
            var rect = GetSelectionRect();
            using (var pen = new Pen(Color.FromArgb(0, 120, 215), 1))
            {
                pen.DashStyle = DashStyle.Dash;
                g.DrawRectangle(pen, rect);
            }
            using (var brush = new SolidBrush(Color.FromArgb(20, 0, 120, 215)))
            {
                g.FillRectangle(brush, rect);
            }
        }

        private Rectangle GetSelectionRect()
        {
            return new Rectangle(
                Math.Min(startPoint.X, currentPoint.X),
                Math.Min(startPoint.Y, currentPoint.Y),
                Math.Abs(currentPoint.X - startPoint.X),
                Math.Abs(currentPoint.Y - startPoint.Y)
            );
        }
    }
}
