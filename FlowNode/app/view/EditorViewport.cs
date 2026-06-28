using System;
using System.Collections.Generic;
using System.Drawing;

namespace FlowNode.app.view
{
    /// <summary>
    /// 画布世界坐标与控件客户区坐标换算，以及脏区包围盒计算（供局部 Invalidate 使用）。
    /// </summary>
    public static class EditorViewport
    {
        public const float NodeVisualMargin = 12f;
        public const float ConnectorMargin = 8f;

        public static RectangleF ExpandNodeBounds(Rectangle bounds, float margin = NodeVisualMargin)
        {
            var rect = RectangleF.FromLTRB(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
            rect.Inflate(margin, margin);
            return rect;
        }

        public static RectangleF GetConnectorWorldBounds(Point startPoint, Point endPoint, float margin = ConnectorMargin)
        {
            float minX = Math.Min(startPoint.X, endPoint.X);
            float minY = Math.Min(startPoint.Y, endPoint.Y);
            float maxX = Math.Max(startPoint.X, endPoint.X);
            float maxY = Math.Max(startPoint.Y, endPoint.Y);

            float tangentLength = Math.Min(100, Math.Abs(endPoint.X - startPoint.X) * 0.5f);
            maxX = Math.Max(maxX, startPoint.X + tangentLength);
            minX = Math.Min(minX, endPoint.X - tangentLength);

            var rect = RectangleF.FromLTRB(minX, minY, maxX, maxY);
            rect.Inflate(margin, margin);
            return rect;
        }

        public static RectangleF Union(RectangleF a, RectangleF b)
        {
            if (a.Width <= 0 || a.Height <= 0)
                return b;
            if (b.Width <= 0 || b.Height <= 0)
                return a;

            return RectangleF.FromLTRB(
                Math.Min(a.Left, b.Left),
                Math.Min(a.Top, b.Top),
                Math.Max(a.Right, b.Right),
                Math.Max(a.Bottom, b.Bottom));
        }

        public static RectangleF UnionAll(IEnumerable<RectangleF> rects)
        {
            RectangleF? union = null;
            foreach (var rect in rects)
            {
                if (rect.Width <= 0 || rect.Height <= 0)
                    continue;

                union = union.HasValue ? Union(union.Value, rect) : rect;
            }

            return union ?? RectangleF.Empty;
        }

        public static Rectangle WorldToClientRect(RectangleF worldRect, float zoom, Point panOffset, Size controlSize)
        {
            if (worldRect.Width <= 0 || worldRect.Height <= 0)
                return Rectangle.Empty;

            int left = (int)Math.Floor(worldRect.Left * zoom + panOffset.X);
            int top = (int)Math.Floor(worldRect.Top * zoom + panOffset.Y);
            int right = (int)Math.Ceiling(worldRect.Right * zoom + panOffset.X);
            int bottom = (int)Math.Ceiling(worldRect.Bottom * zoom + panOffset.Y);

            var clientRect = Rectangle.FromLTRB(left, top, right, bottom);
            clientRect.Intersect(new Rectangle(0, 0, controlSize.Width, controlSize.Height));
            return clientRect.Width > 0 && clientRect.Height > 0 ? clientRect : Rectangle.Empty;
        }
    }
}
