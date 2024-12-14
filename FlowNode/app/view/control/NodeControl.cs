using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace FlowNode.app.view
{
    public abstract class NodeControl
    {
        public Rectangle Bounds { get; set; }
        public string Name { get; set; }
        public bool Enabled { get; set; } = true;
        public bool Visible { get; set; } = true;
        public NodeView ParentNode { get; set; }

        protected NodeControl(NodeView parentNode, string name)
        {
            ParentNode = parentNode;
            Name = name;
        }

        public abstract void Paint(Graphics g);
        public abstract void OnMouseDown(Point location, MouseButtons button);
        public abstract void OnMouseUp(Point location, MouseButtons button);
        public abstract void OnMouseMove(Point location);
    }
}
