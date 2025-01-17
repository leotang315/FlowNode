using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace FlowNode
{
    public class SelectingState : EditorState
    {
        private readonly Point selectionStart;
        private Point currentPoint;

        public SelectingState(NodeEditor editor, Point start) : base(editor)
        {
            selectionStart = start;
            currentPoint = start;
        }

        public override void OnMouseMove(MouseEventArgs e)
        {
            currentPoint = ScreenToNode(e.Location);
            var selectionRect = GetSelectionRect();

            Editor.ClearSelection();
            foreach (var nodeView in Editor.NodeViews.Values)
            {
                if (selectionRect.IntersectsWith(nodeView.Bounds))
                {
                    Editor.AddToSelection(nodeView);
                }
            }
            Editor.Invalidate();
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
                Math.Min(selectionStart.X, currentPoint.X),
                Math.Min(selectionStart.Y, currentPoint.Y),
                Math.Abs(currentPoint.X - selectionStart.X),
                Math.Abs(currentPoint.Y - selectionStart.Y)
            );
        }
    }
}