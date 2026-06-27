using System;
using System.Drawing;
using System.Windows.Forms;

namespace FlowNode
{
    public class IdleState : EditorState
    {
        public IdleState(NodeEditor editor) : base(editor) { }

        public override string getName()
        {
            return "IdleState";
        }

        public override void OnMouseDown(MouseEventArgs e)
        {
            var mousePos = ScreenToNode(e.Location);
            var (nodeView, pin, connector) = Editor.HitTest(mousePos);

            if (e.Button == MouseButtons.Left)
            {
                Editor.SetSelectedConnector(connector);

                if (pin != null)
                {
                    Editor.ChangeState(new ConnectingState(Editor, pin));
                }
                else if (nodeView != null)
                {
                    bool isCtrlPressed = (Control.ModifierKeys & Keys.Control) == Keys.Control;
                    
                    if (isCtrlPressed)
                    {
                        if (Editor.SelectedNodes.Contains(nodeView))
                        {
                            Editor.RemoveFromSelection(nodeView);
                        }
                        else
                        {
                            Editor.AddToSelection(nodeView);
                        }
                        Editor.Invalidate();
                    }
                    else
                    {
                        if (!Editor.SelectedNodes.Contains(nodeView))
                        {
                            Editor.ClearSelection();
                            Editor.AddToSelection(nodeView);
                        }
                        Editor.ChangeState(new DraggingNodeState(Editor, nodeView, mousePos));
                    }
                }
                else
                {
                    Editor.ClearSelection();
                    Editor.ChangeState(new SelectingState(Editor, mousePos));
                }
            }
            else if (e.Button == MouseButtons.Middle)
            {
                Editor.ChangeState(new PanningState(Editor, e.Location));
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (connector != null)
                {
                    Editor.RemoveConnector(connector);
                }
                else if (nodeView == null && pin == null)
                {
                    // 右键空白处弹出节点搜索菜单，在鼠标所在画布位置创建节点
                    var menu = new NodeSearchMenu(Editor, Editor.PointToScreen(e.Location), mousePos);
                    menu.Show();
                }
            }
        }
    }
}