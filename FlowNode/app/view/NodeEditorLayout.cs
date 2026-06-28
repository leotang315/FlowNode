using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using FlowNode.app.view;

namespace FlowNode
{
    public enum NodeAlignEdge
    {
        Left,
        Right,
        Top,
        Bottom
    }

    /// <summary>
    /// 多选节点布局计算（对齐/等距），供 NodeEditor 与单元测试共用。
    /// </summary>
    public static class NodeEditorLayout
    {
        public static Dictionary<NodeView, Point> Align(IReadOnlyList<NodeView> views, NodeAlignEdge edge)
        {
            var result = new Dictionary<NodeView, Point>();
            if (views == null || views.Count < 2)
                return result;

            switch (edge)
            {
                case NodeAlignEdge.Left:
                    {
                        int target = views.Min(v => v.Bounds.Left);
                        foreach (var view in views)
                        {
                            var loc = view.Bounds.Location;
                            if (loc.X != target)
                                result[view] = new Point(target, loc.Y);
                        }
                        break;
                    }
                case NodeAlignEdge.Right:
                    {
                        int target = views.Max(v => v.Bounds.Right);
                        foreach (var view in views)
                        {
                            int newX = target - view.Bounds.Width;
                            if (view.Bounds.X != newX)
                                result[view] = new Point(newX, view.Bounds.Y);
                        }
                        break;
                    }
                case NodeAlignEdge.Top:
                    {
                        int target = views.Min(v => v.Bounds.Top);
                        foreach (var view in views)
                        {
                            if (view.Bounds.Y != target)
                                result[view] = new Point(view.Bounds.X, target);
                        }
                        break;
                    }
                case NodeAlignEdge.Bottom:
                    {
                        int target = views.Max(v => v.Bounds.Bottom);
                        foreach (var view in views)
                        {
                            int newY = target - view.Bounds.Height;
                            if (view.Bounds.Y != newY)
                                result[view] = new Point(view.Bounds.X, newY);
                        }
                        break;
                    }
            }

            return result;
        }

        public static Dictionary<NodeView, Point> DistributeHorizontally(IReadOnlyList<NodeView> views)
        {
            var result = new Dictionary<NodeView, Point>();
            if (views == null || views.Count < 3)
                return result;

            var ordered = views.OrderBy(v => v.Bounds.Left).ToList();
            int left = ordered[0].Bounds.Left;
            int right = ordered[ordered.Count - 1].Bounds.Right;
            int totalWidth = ordered.Sum(v => v.Bounds.Width);
            int span = right - left - totalWidth;
            if (span < 0)
                return result;

            int gap = span / (ordered.Count - 1);
            int x = left;
            for (int i = 0; i < ordered.Count; i++)
            {
                var view = ordered[i];
                var newLoc = new Point(x, view.Bounds.Y);
                if (view.Bounds.Location != newLoc)
                    result[view] = newLoc;
                x += view.Bounds.Width + gap;
            }

            return result;
        }

        public static Dictionary<NodeView, Point> DistributeVertically(IReadOnlyList<NodeView> views)
        {
            var result = new Dictionary<NodeView, Point>();
            if (views == null || views.Count < 3)
                return result;

            var ordered = views.OrderBy(v => v.Bounds.Top).ToList();
            int top = ordered[0].Bounds.Top;
            int bottom = ordered[ordered.Count - 1].Bounds.Bottom;
            int totalHeight = ordered.Sum(v => v.Bounds.Height);
            int span = bottom - top - totalHeight;
            if (span < 0)
                return result;

            int gap = span / (ordered.Count - 1);
            int y = top;
            for (int i = 0; i < ordered.Count; i++)
            {
                var view = ordered[i];
                var newLoc = new Point(view.Bounds.X, y);
                if (view.Bounds.Location != newLoc)
                    result[view] = newLoc;
                y += view.Bounds.Height + gap;
            }

            return result;
        }
    }
}
