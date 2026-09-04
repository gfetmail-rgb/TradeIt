using System;
using System.Collections.Generic;
using System.Linq;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private enum DrawingHandleKind
        {
            TrendLineA, TrendLineB,
            HorizontalLine,
            VerticalLine,
            RayA, RayB,
            ParallelA, ParallelB, ParallelC,
            RectangleTopLeft, RectangleTopRight, RectangleBottomRight, RectangleBottomLeft,
            PitchforkA, PitchforkB, PitchforkC,
            FibonacciA, FibonacciB, FibonacciC
        }

        private sealed class DrawingHandleInfo
        {
            public DrawingHandleKind Kind { get; init; }
            public ScottPlot.Coordinates Point { get; init; }
        }

        private readonly List<ScottPlot.Plottables.Marker> _drawingSelectionHandles = new();
        private readonly List<ScottPlot.IPlottable> _drawingSelectionOverlays = new();
        private DrawingHandleKind? _activeDrawingHandle;

        private const string SelectedDrawingColor = "#00BFFF";
        private const float SelectedDrawingWidth = 3.0f;
        private const float DrawingHandleSize = 12.0f;

        private void ClearDrawingSelectionVisuals()
        {
            foreach (var marker in _drawingSelectionHandles)
                Chart.Plot.Remove(marker);
            _drawingSelectionHandles.Clear();

            foreach (var overlay in _drawingSelectionOverlays)
                Chart.Plot.Remove(overlay);
            _drawingSelectionOverlays.Clear();
            _activeDrawingHandle = null;
        }

        private void RenderDrawingSelectionOverlay()
        {
            ClearDrawingSelectionVisuals();
            if (_selectedDrawing == null) return;

            ScottPlot.Color color = ScottPlot.Color.FromHtml(SelectedDrawingColor);
            foreach (var line in GetSelectedDrawingOverlayLines())
            {
                line.LineColor = color;
                line.LineWidth = SelectedDrawingWidth;
                _drawingSelectionOverlays.Add(line);
            }

            foreach (DrawingHandleInfo handle in GetDrawingHandles())
            {
                var marker = Chart.Plot.Add.Marker(handle.Point.X, handle.Point.Y, ScottPlot.MarkerShape.FilledCircle);
                marker.MarkerSize = DrawingHandleSize;
                marker.MarkerFillColor = color;
                marker.MarkerLineColor = ScottPlot.Color.FromHtml("#FFFFFF");
                marker.LineWidth = 1.5f;
                _drawingSelectionHandles.Add(marker);
            }
        }

        private List<ScottPlot.Plottables.Scatter> GetSelectedDrawingOverlayLines()
        {
            var result = new List<ScottPlot.Plottables.Scatter>();
            if (_selectedDrawing == null) return result;

            var limits = Chart.Plot.Axes.GetLimits();

            switch (_selectedDrawingKind)
            {
                case DrawingSelectionKind.TrendLine:
                    {
                        var d = (TrendLineDrawing)_selectedDrawing;
                        result.Add(AddSelectionLine(d.X1, d.Y1, d.X2, d.Y2));
                        break;
                    }
                case DrawingSelectionKind.HorizontalLine:
                    {
                        var d = (HorizontalLineDrawing)_selectedDrawing;
                        result.Add(AddSelectionLine(limits.Left, d.Y, limits.Right, d.Y));
                        break;
                    }
                case DrawingSelectionKind.VerticalLine:
                    {
                        var d = (VerticalLineDrawing)_selectedDrawing;
                        result.Add(AddSelectionLine(d.X, limits.Bottom, d.X, limits.Top));
                        break;
                    }
                case DrawingSelectionKind.Ray:
                    {
                        var d = (RayDrawing)_selectedDrawing;
                        GetRayEnd(d.X1, d.Y1, d.X2, d.Y2, limits, out double endX, out double endY);
                        result.Add(AddSelectionLine(d.X1, d.Y1, endX, endY));
                        break;
                    }
                case DrawingSelectionKind.ParallelChannel:
                    {
                        var d = (ParallelChannelDrawing)_selectedDrawing;
                        result.Add(AddSelectionLine(d.A.X, d.A.Y, d.B.X, d.B.Y));
                        double dx = d.B.X - d.A.X;
                        double dy = d.B.Y - d.A.Y;
                        result.Add(AddSelectionLine(d.C.X, d.C.Y, d.C.X + dx, d.C.Y + dy));
                        break;
                    }
                case DrawingSelectionKind.Rectangle:
                    {
                        var d = (RectangleDrawing)_selectedDrawing;
                        GetRectangleCorners(d, out var tl, out var tr, out var br, out var bl);
                        result.Add(AddSelectionLine(tl.X, tl.Y, tr.X, tr.Y));
                        result.Add(AddSelectionLine(tr.X, tr.Y, br.X, br.Y));
                        result.Add(AddSelectionLine(br.X, br.Y, bl.X, bl.Y));
                        result.Add(AddSelectionLine(bl.X, bl.Y, tl.X, tl.Y));
                        break;
                    }
                case DrawingSelectionKind.Pitchfork:
                    {
                        var d = (PitchforkDrawing)_selectedDrawing;
                        var target = Midpoint(d.B, d.C);
                        AddSelectionRay(result, d.A, target, limits);
                        AddSelectionRay(result, d.B, target, limits);
                        AddSelectionRay(result, d.C, target, limits);
                        break;
                    }
                case DrawingSelectionKind.Fibonacci:
                    {
                        var d = (FibonacciDrawing)_selectedDrawing;
                        double ab = d.B.Y - d.A.Y;
                        double[] ratios = d.IsExtension
                            ? new[] { 0.0, 0.382, 0.618, 1.0, 1.618, 2.618 }
                            : new[] { 0.0, 0.236, 0.382, 0.5, 0.618, 0.786, 1.0 };
                        foreach (double ratio in ratios)
                        {
                            double y = d.IsExtension ? d.C.Y + ab * ratio : d.B.Y - ab * ratio;
                            double left = d.IsExtension ? Math.Min(d.A.X, d.C.X) : Math.Min(d.A.X, d.B.X);
                            double right = d.IsExtension ? Math.Max(d.A.X, d.C.X) : Math.Max(d.A.X, d.B.X);
                            result.Add(AddSelectionLine(left, y, right, y));
                        }
                        break;
                    }
            }
            return result;
        }

        private ScottPlot.Plottables.Scatter AddSelectionLine(double x1, double y1, double x2, double y2)
        {
            var line = Chart.Plot.Add.ScatterLine(new[] { x1, x2 }, new[] { y1, y2 });
            line.MarkerSize = 0;
            return line;
        }

        private void AddSelectionRay(List<ScottPlot.Plottables.Scatter> lines, ScottPlot.Coordinates start, ScottPlot.Coordinates through, ScottPlot.AxisLimits limits)
        {
            GetRayEnd(start.X, start.Y, through.X, through.Y, limits, out double endX, out double endY);
            lines.Add(AddSelectionLine(start.X, start.Y, endX, endY));
        }

        private static void GetRayEnd(double x1, double y1, double x2, double y2, ScottPlot.AxisLimits limits, out double endX, out double endY)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            if (Math.Abs(dx) < 1e-12)
            {
                endX = x1;
                endY = dy >= 0 ? limits.Top : limits.Bottom;
                return;
            }
            endX = dx >= 0 ? limits.Right : limits.Left;
            endY = y1 + dy / dx * (endX - x1);
        }

        private List<DrawingHandleInfo> GetDrawingHandles()
        {
            var result = new List<DrawingHandleInfo>();
            if (_selectedDrawing == null) return result;

            switch (_selectedDrawingKind)
            {
                case DrawingSelectionKind.TrendLine:
                    {
                        var d = (TrendLineDrawing)_selectedDrawing;
                        result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.TrendLineA, Point = new(d.X1, d.Y1) });
                        result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.TrendLineB, Point = new(d.X2, d.Y2) });
                        break;
                    }
                case DrawingSelectionKind.HorizontalLine:
                    {
                        var d = (HorizontalLineDrawing)_selectedDrawing;
                        var limits = Chart.Plot.Axes.GetLimits();
                        double x = (limits.Left + limits.Right) / 2.0;
                        result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.HorizontalLine, Point = new(x, d.Y) });
                        break;
                    }
                case DrawingSelectionKind.VerticalLine:
                    {
                        var d = (VerticalLineDrawing)_selectedDrawing;
                        var limits = Chart.Plot.Axes.GetLimits();
                        double y = (limits.Bottom + limits.Top) / 2.0;
                        result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.VerticalLine, Point = new(d.X, y) });
                        break;
                    }
                case DrawingSelectionKind.Ray:
                    {
                        var d = (RayDrawing)_selectedDrawing;
                        result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.RayA, Point = new(d.X1, d.Y1) });
                        result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.RayB, Point = new(d.X2, d.Y2) });
                        break;
                    }
                case DrawingSelectionKind.ParallelChannel:
                    {
                        var d = (ParallelChannelDrawing)_selectedDrawing;
                        result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.ParallelA, Point = d.A });
                        result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.ParallelB, Point = d.B });
                        result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.ParallelC, Point = d.C });
                        break;
                    }
                case DrawingSelectionKind.Rectangle:
                    {
                        var d = (RectangleDrawing)_selectedDrawing;
                        GetRectangleCorners(d, out var tl, out var tr, out var br, out var bl);
                        result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.RectangleTopLeft, Point = tl });
                        result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.RectangleTopRight, Point = tr });
                        result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.RectangleBottomRight, Point = br });
                        result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.RectangleBottomLeft, Point = bl });
                        break;
                    }
                case DrawingSelectionKind.Pitchfork:
                    {
                        var d = (PitchforkDrawing)_selectedDrawing;
                        result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.PitchforkA, Point = d.A });
                        result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.PitchforkB, Point = d.B });
                        result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.PitchforkC, Point = d.C });
                        break;
                    }
                case DrawingSelectionKind.Fibonacci:
                    {
                        var d = (FibonacciDrawing)_selectedDrawing;
                        result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.FibonacciA, Point = d.A });
                        result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.FibonacciB, Point = d.B });
                        if (d.IsExtension)
                            result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.FibonacciC, Point = d.C });
                        break;
                    }
            }
            return result;
        }

        private static void GetRectangleCorners(RectangleDrawing d,
            out ScottPlot.Coordinates topLeft,
            out ScottPlot.Coordinates topRight,
            out ScottPlot.Coordinates bottomRight,
            out ScottPlot.Coordinates bottomLeft)
        {
            double left = Math.Min(d.A.X, d.B.X);
            double right = Math.Max(d.A.X, d.B.X);
            double bottom = Math.Min(d.A.Y, d.B.Y);
            double top = Math.Max(d.A.Y, d.B.Y);
            topLeft = new(left, top);
            topRight = new(right, top);
            bottomRight = new(right, bottom);
            bottomLeft = new(left, bottom);
        }

        private bool TryGetHandleAtPoint(ScottPlot.Coordinates point, out DrawingHandleKind handleKind)
        {
            handleKind = default;
            if (_selectedDrawing == null) return false;

            double tolerance = GetHandleHitTolerance();
            DrawingHandleInfo? nearest = null;
            double nearestDistance = double.MaxValue;
            foreach (DrawingHandleInfo handle in GetDrawingHandles())
            {
                double distance = Math.Sqrt(
                    Math.Pow(point.X - handle.Point.X, 2) +
                    Math.Pow(point.Y - handle.Point.Y, 2));
                if (distance <= tolerance && distance < nearestDistance)
                {
                    nearest = handle;
                    nearestDistance = distance;
                }
            }

            if (nearest == null) return false;
            handleKind = nearest.Kind;
            return true;
        }

        private double GetHandleHitTolerance()
        {
            var limits = Chart.Plot.Axes.GetLimits();
            double width = Math.Max(1.0, Chart.ActualWidth);
            double height = Math.Max(1.0, Chart.ActualHeight);
            double tx = Math.Abs(limits.Right - limits.Left) * 9.0 / width;
            double ty = Math.Abs(limits.Top - limits.Bottom) * 9.0 / height;
            return Math.Max(tx, ty);
        }

        private bool MoveSelectedHandle(DrawingHandleKind handleKind, ScottPlot.Coordinates point)
        {
            if (_selectedDrawing == null) return false;

            switch (_selectedDrawingKind)
            {
                case DrawingSelectionKind.TrendLine:
                    {
                        var d = (TrendLineDrawing)_selectedDrawing;
                        RemovePlotLine(d.PlotLine);
                        if (handleKind == DrawingHandleKind.TrendLineA) { d.X1 = point.X; d.Y1 = point.Y; }
                        else { d.X2 = point.X; d.Y2 = point.Y; }
                        AddTrendLineToChart(d);
                        break;
                    }
                case DrawingSelectionKind.HorizontalLine:
                    {
                        var d = (HorizontalLineDrawing)_selectedDrawing;
                        RemovePlotLine(d.PlotLine);
                        d.Y = point.Y;
                        AddHorizontalLineToChart(d);
                        break;
                    }
                case DrawingSelectionKind.VerticalLine:
                    {
                        var d = (VerticalLineDrawing)_selectedDrawing;
                        RemovePlotLine(d.PlotLine);
                        d.X = point.X;
                        AddVerticalLineToChart(d);
                        break;
                    }
                case DrawingSelectionKind.Ray:
                    {
                        var d = (RayDrawing)_selectedDrawing;
                        RemovePlotLine(d.PlotLine);
                        if (handleKind == DrawingHandleKind.RayA) { d.X1 = point.X; d.Y1 = point.Y; }
                        else { d.X2 = point.X; d.Y2 = point.Y; }
                        AddRayToChart(d);
                        break;
                    }
                case DrawingSelectionKind.ParallelChannel:
                    {
                        var d = (ParallelChannelDrawing)_selectedDrawing;
                        RemovePlotLine(d.BaseLine);
                        RemovePlotLine(d.ParallelLine);
                        if (handleKind == DrawingHandleKind.ParallelA) d.A = point;
                        else if (handleKind == DrawingHandleKind.ParallelB) d.B = point;
                        else d.C = point;
                        AddParallelChannelToChart(d);
                        break;
                    }
                case DrawingSelectionKind.Rectangle:
                    {
                        var d = (RectangleDrawing)_selectedDrawing;
                        GetRectangleCorners(d, out var tl, out var tr, out var br, out var bl);
                        ScottPlot.Coordinates opposite;
                        switch (handleKind)
                        {
                            case DrawingHandleKind.RectangleTopLeft: opposite = br; break;
                            case DrawingHandleKind.RectangleTopRight: opposite = bl; break;
                            case DrawingHandleKind.RectangleBottomRight: opposite = tl; break;
                            default: opposite = tr; break;
                        }
                        RemoveRectangleLines(d);
                        d.A = point;
                        d.B = opposite;
                        AddRectangleToChart(d);
                        break;
                    }
                case DrawingSelectionKind.Pitchfork:
                    {
                        var d = (PitchforkDrawing)_selectedDrawing;
                        RemovePitchforkLines(d);
                        if (handleKind == DrawingHandleKind.PitchforkA) d.A = point;
                        else if (handleKind == DrawingHandleKind.PitchforkB) d.B = point;
                        else d.C = point;
                        AddPitchforkToChart(d);
                        break;
                    }
                case DrawingSelectionKind.Fibonacci:
                    {
                        var d = (FibonacciDrawing)_selectedDrawing;
                        RemoveFibonacciLines(d);
                        if (handleKind == DrawingHandleKind.FibonacciA) d.A = point;
                        else if (handleKind == DrawingHandleKind.FibonacciB) d.B = point;
                        else d.C = point;
                        RenderFibonacciDrawing(d);
                        break;
                    }
                default:
                    return false;
            }

            RenderDrawingSelectionOverlay();
            return true;
        }

        private void DeleteSelectedDrawingAndClearVisuals()
        {
            DeleteSelectedDrawing();
        }
    }
}
