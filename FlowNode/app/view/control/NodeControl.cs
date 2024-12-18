using System;
using System.Drawing;
using System.Windows.Forms;

namespace FlowNode.app.view
{
    public abstract class NodeControl
    {
        #region Properties
        public virtual Rectangle Bounds { get; set; }
        public string Name { get; set; }
        public bool Enabled { get; set; } = true;
        public bool Visible { get; set; } = true;
        public NodeView ParentNode { get; set; }
        public bool IsFocused { get; protected set; }
        public bool IsHovered { get; protected set; }
        public bool IsPressed { get; protected set; }
        #endregion

        #region Events
        // 鼠标事件
        public event EventHandler<MouseEventArgs> MouseDown;
        public event EventHandler<MouseEventArgs> MouseUp;
        public event EventHandler<MouseEventArgs> MouseMove;
        public event EventHandler<MouseEventArgs> MouseWheel;
        public event EventHandler<EventArgs> MouseEnter;
        public event EventHandler<EventArgs> MouseLeave;
        public event EventHandler<EventArgs> MouseHover;
        public event EventHandler<EventArgs> Click;

        // 键盘事件
        public event EventHandler<KeyPressEventArgs> KeyPress;
        public event EventHandler<KeyEventArgs> KeyDown;
        public event EventHandler<KeyEventArgs> KeyUp;

        // 状态变化事件
        public event EventHandler<EventArgs> GotFocus;
        public event EventHandler<EventArgs> LostFocus;
        public event EventHandler<EventArgs> EnabledChanged;
        public event EventHandler<EventArgs> VisibleChanged;
        #endregion

        #region Constructor
        protected NodeControl(NodeView parentNode, string name)
        {
            ParentNode = parentNode;
            Name = name;
        }
        #endregion

        #region Public Methods
        public abstract void Paint(Graphics g);

        public virtual void Focus()
        {
            if (!IsFocused && Enabled && Visible)
            {
                IsFocused = true;
                GotFocus?.Invoke(this, EventArgs.Empty);
                ParentNode?.Invalidate();
            }
        }

        public virtual void LoseFocus()
        {
            if (IsFocused)
            {
                IsFocused = false;
                LostFocus?.Invoke(this, EventArgs.Empty);
                ParentNode?.Invalidate();
            }
        }

        public void SetEnabled(bool enabled)
        {
            if (Enabled != enabled)
            {
                Enabled = enabled;
                EnabledChanged?.Invoke(this, EventArgs.Empty);
                ParentNode?.Invalidate();
            }
        }

        public void SetVisible(bool visible)
        {
            if (Visible != visible)
            {
                Visible = visible;
                VisibleChanged?.Invoke(this, EventArgs.Empty);
                ParentNode?.Invalidate();
            }
        }
        #endregion

        #region Event Handlers
        public virtual void OnMouseDown(Point location, MouseButtons button)
        {
            if (!Enabled || !Visible) return;

            IsPressed = true;
            Focus();
            MouseDown?.Invoke(this, new MouseEventArgs(button, 1, location.X, location.Y, 0));
            ParentNode?.Invalidate();
        }

        public virtual void OnMouseUp(Point location, MouseButtons button)
        {
            if (!Enabled || !Visible) return;

            if (IsPressed && Bounds.Contains(location))
            {
                Click?.Invoke(this, EventArgs.Empty);
            }

            IsPressed = false;
            MouseUp?.Invoke(this, new MouseEventArgs(button, 1, location.X, location.Y, 0));
            ParentNode?.Invalidate();
        }

        public virtual void OnMouseMove(Point location)
        {
            if (!Enabled || !Visible) return;

            bool newHovered = Bounds.Contains(location);
            if (newHovered != IsHovered)
            {
                IsHovered = newHovered;
                if (IsHovered)
                {
                    MouseEnter?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    MouseLeave?.Invoke(this, EventArgs.Empty);
                }
                ParentNode?.Invalidate();
            }

            if (IsHovered)
            {
                MouseHover?.Invoke(this, EventArgs.Empty);
            }

            MouseMove?.Invoke(this, new MouseEventArgs(MouseButtons.None, 0, location.X, location.Y, 0));
        }

        public virtual void OnKeyPress(KeyPressEventArgs e)
        {
            if (!Enabled || !Visible || !IsFocused) return;
            KeyPress?.Invoke(this, e);
        }

        public virtual void OnKeyDown(KeyEventArgs e)
        {
            if (!Enabled || !Visible || !IsFocused) return;
            KeyDown?.Invoke(this, e);
        }

        public virtual void OnKeyUp(KeyEventArgs e)
        {
            if (!Enabled || !Visible || !IsFocused) return;
            KeyUp?.Invoke(this, e);
        }

        public virtual void OnMouseWheel(MouseEventArgs e)
        {
            if (!Enabled || !Visible || !IsFocused) return;
            MouseWheel?.Invoke(this, e);
        }
        #endregion
    }
}
