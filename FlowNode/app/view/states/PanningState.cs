using System;
using System.Drawing;
using System.Windows.Forms;
namespace FlowNode
{
    public class PanningState : EditorState
    {
        private Point lastMousePos;

        public override string getName()
        {
            return "PanningState";
        }

        public PanningState(NodeEditor editor, Point startPos) : base(editor)
        {
            lastMousePos = startPos;
        }

        public override void OnMouseMove(MouseEventArgs e)
        {
            Editor.PanOffset = new Point(
                Editor.PanOffset.X + (e.X - lastMousePos.X),
                Editor.PanOffset.Y + (e.Y - lastMousePos.Y)
            );
            lastMousePos = e.Location;
            Editor.Invalidate();
        }

        public override void OnMouseUp(MouseEventArgs e)
        {
            Editor.ChangeState(new IdleState(Editor));
        }
    }
}