using System;
using System.Drawing;
using System.Windows.Forms;

namespace FlowNode
{
    public abstract class EditorState
    {
        protected NodeEditor Editor { get; }

        protected EditorState(NodeEditor editor)
        {
            Editor = editor;
        }

        public virtual void OnMouseDown(MouseEventArgs e) { }
        public virtual void OnMouseMove(MouseEventArgs e) { }
        public virtual void OnMouseUp(MouseEventArgs e) { }
        public virtual void OnPaint(Graphics g) { }
        public virtual void OnKeyPress(KeyPressEventArgs e) { }
        public virtual void OnKeyDown(KeyEventArgs e) { }

        protected Point ScreenToNode(Point screenPos)
        {
            return Editor.ScreenToNode(screenPos);
        }
    }
}