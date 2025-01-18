using System;
using System.Drawing;
using System.Windows.Forms;
using FlowNode.node;
using FlowNode.app.view;
namespace FlowNode
{
    public class ConnectingState : EditorState
    {
        private readonly Pin sourcePin;
        private readonly Point connectingStart;
        private Point connectingEnd;
        private Pin hoveredPin;
        public override string getName()
        {
            return "ConnectingState";
        }
        public ConnectingState(NodeEditor editor, Pin sourcePin) : base(editor)
        {
            this.sourcePin = sourcePin;
            this.connectingStart = GetPinConnectionPoint(sourcePin);
            this.connectingEnd = connectingStart;
        }

        public override void OnMouseMove(MouseEventArgs e)
        {
            connectingEnd = ScreenToNode(e.Location);
            var (_, pin, _) = Editor.HitTest(connectingEnd);
            if (hoveredPin != pin)
            {
                hoveredPin = pin;
                Editor.Invalidate();
            }
        }

        public override void OnMouseUp(MouseEventArgs e)
        {
            if (hoveredPin != null && CanConnect(sourcePin, hoveredPin))
            {
                Pin source = sourcePin.direction == PinDirection.Output ? sourcePin : hoveredPin;
                Pin target = sourcePin.direction == PinDirection.Output ? hoveredPin : sourcePin;
                Editor.AddConnector(source, target);
            }
            Editor.ChangeState(new IdleState(Editor));
        }

        public override void OnPaint(Graphics g)
        {
            Point endPoint = hoveredPin != null ?
                GetPinConnectionPoint(hoveredPin) : connectingEnd;

            float tangentLength = Math.Min(100, Math.Abs(endPoint.X - connectingStart.X) * 0.5f);
            Point control1 = new Point(connectingStart.X + (int)tangentLength, connectingStart.Y);
            Point control2 = new Point(endPoint.X - (int)tangentLength, endPoint.Y);

            bool isCompatible = hoveredPin != null && CanConnect(sourcePin, hoveredPin);

            if (hoveredPin != null && !isCompatible)
            {
                using (var pen = new Pen(Color.Red, 2))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    g.DrawBezier(pen, connectingStart, control1, control2, endPoint);
                }
            }
            else
            {
                using (var pen = new Pen(Color.White, 2))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    g.DrawBezier(pen, connectingStart, control1, control2, endPoint);
                }
            }

            if (hoveredPin != null)
            {
                var nodeView = Editor.NodeViews[hoveredPin.host];
                var bounds = nodeView.PinBounds[hoveredPin];
                var pinColor = isCompatible ? Color.FromArgb(0, 120, 215) : Color.FromArgb(255, 0, 0);
                using (var glowBrush = new SolidBrush(Color.FromArgb(100, pinColor)))
                {
                    var glowRect = bounds;
                    glowRect.Inflate(5, 5);
                    g.FillEllipse(glowBrush, glowRect);
                }
            }
        }

        private Point GetPinConnectionPoint(Pin pin)
        {
            if (Editor.NodeViews.TryGetValue(pin.host, out NodeView nodeView) &&
                nodeView.PinBounds.TryGetValue(pin, out Rectangle pinRect))
            {
                return new Point(
                    pin.direction == PinDirection.Input ? pinRect.Left : pinRect.Right,
                    pinRect.Top + pinRect.Height / 2
                );
            }
            return Point.Empty;
        }

        private bool CanConnect(Pin source, Pin target)
        {
            if (source == null || target == null)
                return false;

            // 检查方向和引脚类型
            bool basicCheck = source.direction != target.direction && // 方向相反
                             source.pinType == target.pinType;    // 类型相同

            if (!basicCheck) return false;

            // 如果是执行类型的引脚，不需要检查数据类型
            if (source.pinType == PinType.Execute)
                return true;

            // 确保source是输出引脚，target是输入引脚
            Pin outputPin, inputPin;
            if (source.direction == PinDirection.Output)
            {
                outputPin = source;
                inputPin = target;
            }
            else
            {
                outputPin = target;
                inputPin = source;
            }
            return PinTypeValidator.AreTypesCompatible(outputPin.dataType, inputPin.dataType);
        }
    }
}