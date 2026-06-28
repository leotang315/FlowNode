using System;
using System.Drawing;
using System.Windows.Forms;
using FlowNode.app.view;
using FlowNode.node;

namespace FlowNode
{
    public class ConnectingState : EditorState
    {
        private readonly Pin sourcePin;
        private readonly Point connectingStart;
        private Point connectingEnd;
        private Pin hoveredPin;
        private string hoverError;

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
            hoveredPin = pin;
            hoverError = null;
            if (hoveredPin != null)
            {
                PinTypeValidator.CanConnect(sourcePin, hoveredPin, out hoverError);
            }

            Editor.Invalidate();
        }

        public override void OnMouseUp(MouseEventArgs e)
        {
            if (hoveredPin != null)
            {
                if (PinTypeValidator.CanConnect(sourcePin, hoveredPin, out string error))
                {
                    Pin source = sourcePin.direction == PinDirection.Output ? sourcePin : hoveredPin;
                    Pin target = sourcePin.direction == PinDirection.Output ? hoveredPin : sourcePin;
                    Editor.AddConnector(source, target);
                }
                else
                {
                    Editor.LogEditorMessage("[连线] " + error);
                }
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

            bool isCompatible = hoveredPin != null &&
                PinTypeValidator.CanConnect(sourcePin, hoveredPin, out _);

            if (hoveredPin != null && !isCompatible)
            {
                using (var pen = new Pen(Color.Red, 2))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    g.DrawBezier(pen, connectingStart, control1, control2, endPoint);
                }

                if (!string.IsNullOrEmpty(hoverError))
                {
                    using (var brush = new SolidBrush(Color.FromArgb(255, 180, 80)))
                    using (var font = SystemFonts.DefaultFont)
                    {
                        g.DrawString(hoverError, font, brush, endPoint.X + 8, endPoint.Y + 8);
                    }
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
    }
}
