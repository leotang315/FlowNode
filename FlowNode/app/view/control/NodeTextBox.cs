using System;
using System.Drawing;
using System.Windows.Forms;

namespace FlowNode.app.view
{
    public class NodeTextBox : NodeControl
    {
        public string Text { get; set; }
        public bool IsReadOnly { get; set; }
        public event EventHandler<string> TextChanged;
        private bool isFocused;
        private int cursorPosition;

        public NodeTextBox(NodeView parentNode, string name, string initialText = "") : base(parentNode, name)
        {
            Text = initialText;
            cursorPosition = Text.Length;
        }

        public override void Paint(Graphics g)
        {
            if (!Visible) return;

            // 绘制背景
            using (var brush = new SolidBrush(Color.FromArgb(30, 30, 30)))
            {
                g.FillRectangle(brush, Bounds);
            }

            // 绘制边框
            using (var pen = new Pen(isFocused ? Color.FromArgb(0, 122, 204) : Color.FromArgb(100, 100, 100)))
            {
                g.DrawRectangle(pen, Bounds);
            }

            // 绘制文本
            if (!string.IsNullOrEmpty(Text))
            {
                using (var brush = new SolidBrush(Color.White))
                {
                    var textRect = new Rectangle(Bounds.X + 3, Bounds.Y + 2, 
                                               Bounds.Width - 6, Bounds.Height - 4);
                    g.DrawString(Text, SystemFonts.DefaultFont, brush, textRect);
                }
            }

            // 如果获得焦点，绘制光标
            if (isFocused && !IsReadOnly)
            {
                string textBeforeCursor = Text.Substring(0, cursorPosition);
                float cursorX = Bounds.X + 3;
                if (!string.IsNullOrEmpty(textBeforeCursor))
                {
                    cursorX += g.MeasureString(textBeforeCursor, SystemFonts.DefaultFont).Width;
                }

                using (var pen = new Pen(Color.White))
                {
                    g.DrawLine(pen, 
                        cursorX, Bounds.Y + 2,
                        cursorX, Bounds.Bottom - 2);
                }
            }
        }

        public override void OnMouseDown(Point location, MouseButtons button)
        {
            if (!Enabled || button != MouseButtons.Left) return;
            isFocused = true;
            Focus();

            // 计算点击位置对应的文本位置
            float x = location.X - Bounds.X - 3;
            cursorPosition = 0;
            float totalWidth = 0;
            
            using (var g = ParentNode.CreateGraphics())
            {
                for (int i = 0; i <= Text.Length; i++)
                {
                    float charWidth = 0;
                    if (i < Text.Length)
                    {
                        charWidth = g.MeasureString(Text[i].ToString(), SystemFonts.DefaultFont).Width;
                    }
                    if (x < totalWidth + charWidth / 2)
                    {
                        cursorPosition = i;
                        break;
                    }
                    totalWidth += charWidth;
                    if (i == Text.Length)
                    {
                        cursorPosition = i;
                    }
                }
            }
            
            ParentNode?.Invalidate();
        }

        public override void OnMouseUp(Point location, MouseButtons button) { }
        public override void OnMouseMove(Point location) { }

        public override void OnKeyPress(KeyPressEventArgs e)
        {
            if (!Enabled || !Visible || !isFocused || IsReadOnly) return;

            // 插入字符
            Text = Text.Insert(cursorPosition, e.KeyChar.ToString());
            cursorPosition++;
            TextChanged?.Invoke(this, Text);
            ParentNode?.Invalidate();
        }

        public override void OnKeyDown(KeyEventArgs e)
        {
            if (!Enabled || !Visible || !isFocused || IsReadOnly) return;

            switch (e.KeyCode)
            {
                case Keys.Back:
                    if (cursorPosition > 0)
                    {
                        Text = Text.Remove(cursorPosition - 1, 1);
                        cursorPosition--;
                        TextChanged?.Invoke(this, Text);
                    }
                    break;
                case Keys.Delete:
                    if (cursorPosition < Text.Length)
                    {
                        Text = Text.Remove(cursorPosition, 1);
                        TextChanged?.Invoke(this, Text);
                    }
                    break;
                case Keys.Left:
                    if (cursorPosition > 0)
                    {
                        cursorPosition--;
                    }
                    break;
                case Keys.Right:
                    if (cursorPosition < Text.Length)
                    {
                        cursorPosition++;
                    }
                    break;
            }
            ParentNode?.Invalidate();
        }

        public override void OnKeyUp(KeyEventArgs e)
        {
            // 可以在这里处理按键释放事件
        }

    }
} 