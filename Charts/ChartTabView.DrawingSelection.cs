using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private enum DrawingSelectionKind
        {
            None, TrendLine, HorizontalLine, VerticalLine, Ray, ParallelChannel, Rectangle, Pitchfork, Fibonacci
        }

        private readonly struct DrawingCandidate
        {
            public DrawingSelectionKind Kind { get; init; }
            public object Drawing { get; init; }
        }

        private DrawingSelectionKind _selectedDrawingKind;
        private object? _selectedDrawing;
        private ScottPlot.Coordinates _selectionDragStart;
        private bool _selectionDragging;
        private bool _drawingSelectionAttached;
        private double _lastSelectionX = double.NaN;
        private double _lastSelectionY = double.NaN;
        private int _selectionCycleIndex = -1;
        private int _selectionCycleCount;

        private static readonly bool _drawingSelectionRegistered = RegisterDrawingSelectionHandling();

        private static bool RegisterDrawingSelectionHandling()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(DrawingSelection_Loaded));
            return true;
        }

        private static void DrawingSelection_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.AttachDrawingSelectionHandling();
        }

        private void AttachDrawingSelectionHandling()
        {
            if (_drawingSelectionAttached) return;
            _drawingSelectionAttached = true;
            Chart.PreviewMouseLeftButtonDown += DrawingSelection_MouseDown;
            Chart.PreviewMouseMove += DrawingSelection_MouseMove;
            Chart.PreviewMouseLeftButtonUp += DrawingSelection_MouseUp;
            AddHandler(Keyboard.PreviewKeyDownEvent,
                new System.Windows.Input.KeyEventHandler(DrawingSelection_KeyDown), true);
        }

        private void DrawingSelection_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || _textDrawingActive || _activeDrawingTool != TechnicalDrawingTool.Select) return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates point)) return;

            if (_selectedDrawing != null && TryGetHandleAtPoint(point, out DrawingHandleKind handleKind))
            {
                _activeDrawingHandle = handleKind;
                _selectionDragStart = point;
                _selectionDragging = true;
                Chart.CaptureMouse();
                Chart.UserInputProcessor.IsEnabled = false;
                e.Handled = true;
                return;
            }

            if (TrySelectDrawing(point))
            {
                _selectionDragging = false;
                _activeDrawingHandle = null;
                Chart.ReleaseMouseCapture();
                Chart.UserInputProcessor.IsEnabled = false;
                e.Handled = true;
                Chart.Refresh();
            }
            else
            {
                ClearDrawingSelection();
            }
        }

        private void DrawingSelection_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_selectionDragging || _selectedDrawing == null || _activeDrawingHandle == null || e.LeftButton != MouseButtonState.Pressed)
                return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates point)) return;

            if (Math.Abs(point.X - _selectionDragStart.X) < 1e-15 && Math.Abs(point.Y - _selectionDragStart.Y) < 1e-15)
                return;

            DrawingHandleKind handleKind = _activeDrawingHandle.Value;
            if (MoveSelectedHandle(handleKind, point))
            {
                _selectionDragStart = point;
                _activeDrawingHandle = handleKind;
                e.Handled = true;
                Chart.Refresh();
            }
        }

        private void DrawingSelection_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || !_selectionDragging) return;
            _selectionDragging = false;
            _activeDrawingHandle = null;
            Chart.ReleaseMouseCapture();
            Chart.UserInputProcessor.IsEnabled = true;
            e.Handled = true;
            Chart.Refresh();
        }

        private void DrawingSelection_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Delete && e.Key != Key.Back) return;
            if (_selectedDrawing == null || _activeDrawingTool != TechnicalDrawingTool.Select) return;
            DeleteSelectedDrawing();
            e.Handled = true;
        }

        private bool TrySelectDrawing(ScottPlot.Coordinates point)
        {
            var candidates = GetDrawingCandidates(point);
            if (candidates.Count == 0) return false;

            double cycleTolerance = GetHandleHitTolerance();
            bool sameLocation = !double.IsNaN(_lastSelectionX) &&
                Math.Abs(point.X - _lastSelectionX) <= cycleTolerance &&
                Math.Abs(point.Y - _lastSelectionY) <= cycleTolerance &&
                _selectionCycleCount == candidates.Count;

            _selectionCycleIndex = sameLocation
                ? (_selectionCycleIndex + 1) % candidates.Count
                : 0;

            _lastSelectionX = point.X;
            _lastSelectionY = point.Y;
            _selectionCycleCount = candidates.Count;

            var candidate = candidates[_selectionCycleIndex];
            SelectDrawing(candidate.Kind, candidate.Drawing);
            return true;
        }

        private List<DrawingCandidate> GetDrawingCandidates(ScottPlot.Coordinates point)
        {
            double tolerance = GetDrawingHitTolerance();
            var candidates = new List<DrawingCandidate>();

            for (int i = _fibonacciDrawings.Count - 1; i >= 0; i--)
                if (IsPointOnFibonacciDrawing(point, _fibonacciDrawings[i], tolerance))
                    candidates.Add(new DrawingCandidate { Kind = DrawingSelectionKind.Fibonacci, Drawing = _fibonacciDrawings[i] });

            for (int i = _pitchforks.Count - 1; i >= 0; i--)
                if (DistanceToPitchfork(point, _pitchforks[i]) <= tolerance)
                    candidates.Add(new DrawingCandidate { Kind = DrawingSelectionKind.Pitchfork, Drawing = _pitchforks[i] });

            for (int i = _parallelChannels.Count - 1; i >= 0; i--)
            {
                var d = _parallelChannels[i];
                var parallelEnd = new ScottPlot.Coordinates(d.C.X + d.B.X - d.A.X, d.C.Y + d.B.Y - d.A.Y);
                if (DistancePointToSegment(point, d.A, d.B) <= tolerance ||
                    DistancePointToSegment(point, d.C, parallelEnd) <= tolerance)
                    candidates.Add(new DrawingCandidate { Kind = DrawingSelectionKind.ParallelChannel, Drawing = d });
            }

            for (int i = _drawingRectangles.Count - 1; i >= 0; i--)
            {
                var d = _drawingRectangles[i];
                if (IsPointOnRectangle(point, d, tolerance))
                    candidates.Add(new DrawingCandidate { Kind = DrawingSelectionKind.Rectangle, Drawing = d });
            }

            for (int i = _rays.Count - 1; i >= 0; i--)
            {
                var d = _rays[i];
                if (DistanceToRay(point, d.X1, d.Y1, d.X2, d.Y2) <= tolerance)
                    candidates.Add(new DrawingCandidate { Kind = DrawingSelectionKind.Ray, Drawing = d });
            }

            for (int i = _trendLines.Count - 1; i >= 0; i--)
            {
                var d = _trendLines[i];
                if (DistancePointToSegment(point,
                    new ScottPlot.Coordinates(d.X1, d.Y1),
                    new ScottPlot.Coordinates(d.X2, d.Y2)) <= tolerance)
                    candidates.Add(new DrawingCandidate { Kind = DrawingSelectionKind.TrendLine, Drawing = d });
            }

            for (int i = _horizontalLines.Count - 1; i >= 0; i--)
                if (DistancePointToHorizontalLine(point, _horizontalLines[i].Y) <= tolerance)
                    candidates.Add(new DrawingCandidate { Kind = DrawingSelectionKind.HorizontalLine, Drawing = _horizontalLines[i] });

            for (int i = _verticalLines.Count - 1; i >= 0; i--)
                if (DistancePointToVerticalLine(point, _verticalLines[i].X) <= tolerance)
                    candidates.Add(new DrawingCandidate { Kind = DrawingSelectionKind.VerticalLine, Drawing = _verticalLines[i] });

            return candidates;
        }

        private bool IsPointOnFibonacciDrawing(ScottPlot.Coordinates point, FibonacciDrawing drawing, double tolerance)
        {
            double ab = drawing.B.Y - drawing.A.Y;
            double left = drawing.IsExtension ? Math.Min(drawing.A.X, drawing.C.X) : Math.Min(drawing.A.X, drawing.B.X);
            double right = drawing.IsExtension ? Math.Max(drawing.A.X, drawing.C.X) : Math.Max(drawing.A.X, drawing.B.X);
            if (point.X < left || point.X > right) return false;

            double[] ratios = drawing.IsExtension
                ? new[] { 0.0, 0.382, 0.618, 1.0, 1.618, 2.618 }
                : new[] { 0.0, 0.236, 0.382, 0.5, 0.618, 0.786, 1.0 };

            foreach (double ratio in ratios)
            {
                double y = drawing.IsExtension ? drawing.C.Y + ab * ratio : drawing.B.Y - ab * ratio;
                if (DistancePointToSegment(point, new ScottPlot.Coordinates(left, y), new ScottPlot.Coordinates(right, y)) <= tolerance)
                    return true;
            }
            return false;
        }

        private static bool IsPointOnRectangle(ScottPlot.Coordinates point, RectangleDrawing d, double tolerance)
        {
            GetRectangleCorners(d, out var tl, out var tr, out var br, out var bl);
            if (DistancePointToSegment(point, tl, tr) <= tolerance ||
                DistancePointToSegment(point, tr, br) <= tolerance ||
                DistancePointToSegment(point, br, bl) <= tolerance ||
                DistancePointToSegment(point, bl, tl) <= tolerance)
                return true;

            return false;
        }

        private double GetDrawingHitTolerance() => 9.0;

        private double DistancePointToHorizontalLine(ScottPlot.Coordinates point, double y)
        {
            ScottPlot.Pixel p = Chart.Plot.GetPixel(point);
            ScottPlot.Pixel q = Chart.Plot.GetPixel(new ScottPlot.Coordinates(point.X, y));
            return Math.Abs(p.Y - q.Y);
        }

        private double DistancePointToVerticalLine(ScottPlot.Coordinates point, double x)
        {
            ScottPlot.Pixel p = Chart.Plot.GetPixel(point);
            ScottPlot.Pixel q = Chart.Plot.GetPixel(new ScottPlot.Coordinates(x, point.Y));
            return Math.Abs(p.X - q.X);
        }

        private double DistancePointToSegment(ScottPlot.Coordinates p, ScottPlot.Coordinates a, ScottPlot.Coordinates b)
        {
            ScottPlot.Pixel pp = Chart.Plot.GetPixel(p);
            ScottPlot.Pixel pa = Chart.Plot.GetPixel(a);
            ScottPlot.Pixel pb = Chart.Plot.GetPixel(b);

            double dx = pb.X - pa.X;
            double dy = pb.Y - pa.Y;
            double length2 = dx * dx + dy * dy;
            if (length2 < 1e-12)
                return Math.Sqrt((pp.X - pa.X) * (pp.X - pa.X) + (pp.Y - pa.Y) * (pp.Y - pa.Y));

            double t = ((pp.X - pa.X) * dx + (pp.Y - pa.Y) * dy) / length2;
            t = Math.Max(0, Math.Min(1, t));
            double x = pa.X + t * dx;
            double y = pa.Y + t * dy;
            return Math.Sqrt((pp.X - x) * (pp.X - x) + (pp.Y - y) * (pp.Y - y));
        }

        private double DistanceToRay(ScottPlot.Coordinates p, double x1, double y1, double x2, double y2)
        {
            var limits = Chart.Plot.Axes.GetLimits();
            double dx = x2 - x1;
            double dy = y2 - y1;
            if (Math.Abs(dx) < 1e-12)
            {
                double endY = dy >= 0 ? limits.Top : limits.Bottom;
                return DistancePointToSegment(p, new ScottPlot.Coordinates(x1, y1), new ScottPlot.Coordinates(x1, endY));
            }

            double endX = dx >= 0 ? limits.Right : limits.Left;
            double endY2 = y1 + dy / dx * (endX - x1);
            return DistancePointToSegment(p, new ScottPlot.Coordinates(x1, y1), new ScottPlot.Coordinates(endX, endY2));
        }

        private double DistanceToPitchfork(ScottPlot.Coordinates p, PitchforkDrawing d)
        {
            var target = Midpoint(d.B, d.C);
            return Math.Min(
                DistanceToRay(p, d.A.X, d.A.Y, target.X, target.Y),
                Math.Min(
                    DistanceToRay(p, d.B.X, d.B.Y, target.X, target.Y),
                    DistanceToRay(p, d.C.X, d.C.Y, target.X, target.Y)));
        }

        private void SelectDrawing(DrawingSelectionKind kind, object drawing)
        {
            bool changed = !ReferenceEquals(_selectedDrawing, drawing) || _selectedDrawingKind != kind;
            _selectedDrawingKind = kind;
            _selectedDrawing = drawing;
            if (changed) _selectionCycleIndex = 0;
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | ابزار انتخاب شد؛ نقاط کنترل را جابه‌جا کنید | حذف: Delete";
            RenderDrawingSelectionOverlay();
        }

        private void DeleteSelectedDrawing()
        {
            if (_selectedDrawing == null) return;
            ClearDrawingSelectionVisuals();

            switch (_selectedDrawingKind)
            {
                case DrawingSelectionKind.Fibonacci:
                    { var d = (FibonacciDrawing)_selectedDrawing; RemoveFibonacciLines(d); _fibonacciDrawings.Remove(d); break; }
                case DrawingSelectionKind.HorizontalLine:
                    { var d = (HorizontalLineDrawing)_selectedDrawing; RemovePlotLine(d.PlotLine); _horizontalLines.Remove(d); break; }
                case DrawingSelectionKind.VerticalLine:
                    { var d = (VerticalLineDrawing)_selectedDrawing; RemovePlotLine(d.PlotLine); _verticalLines.Remove(d); break; }
                case DrawingSelectionKind.TrendLine:
                    { var d = (TrendLineDrawing)_selectedDrawing; RemovePlotLine(d.PlotLine); _trendLines.Remove(d); break; }
                case DrawingSelectionKind.Ray:
                    { var d = (RayDrawing)_selectedDrawing; RemovePlotLine(d.PlotLine); _rays.Remove(d); break; }
                case DrawingSelectionKind.ParallelChannel:
                    { var d = (ParallelChannelDrawing)_selectedDrawing; RemovePlotLine(d.BaseLine); RemovePlotLine(d.ParallelLine); _parallelChannels.Remove(d); break; }
                case DrawingSelectionKind.Rectangle:
                    { var d = (RectangleDrawing)_selectedDrawing; RemoveRectangleLines(d); _drawingRectangles.Remove(d); break; }
                case DrawingSelectionKind.Pitchfork:
                    { var d = (PitchforkDrawing)_selectedDrawing; RemovePitchforkLines(d); _pitchforks.Remove(d); break; }
            }
            ClearDrawingSelection();
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | ابزار حذف شد";
            Chart.Refresh();
        }

        private void RemoveRectangleLines(RectangleDrawing d)
        {
            foreach (var line in d.Lines) Chart.Plot.Remove(line);
            d.Lines.Clear();
        }

        private void RemovePitchforkLines(PitchforkDrawing d)
        {
            RemovePlotLine(d.MedianLine); RemovePlotLine(d.UpperLine); RemovePlotLine(d.LowerLine);
            d.MedianLine = null; d.UpperLine = null; d.LowerLine = null;
        }

        private void RemovePlotLine(ScottPlot.Plottables.Scatter? line)
        {
            if (line != null) Chart.Plot.Remove(line);
        }

        private void RemovePlotLine(ScottPlot.Plottables.HorizontalLine? line)
        {
            if (line != null) Chart.Plot.Remove(line);
        }

        private void RemovePlotLine(ScottPlot.Plottables.VerticalLine? line)
        {
            if (line != null) Chart.Plot.Remove(line);
        }

        private void ClearDrawingSelection()
        {
            ClearDrawingSelectionVisuals();
            _selectedDrawing = null;
            _selectedDrawingKind = DrawingSelectionKind.None;
            _selectionDragging = false;
            _activeDrawingHandle = null;
            _selectionCycleIndex = -1;
            _selectionCycleCount = 0;
            _lastSelectionX = double.NaN;
            _lastSelectionY = double.NaN;
            Chart.UserInputProcessor.IsEnabled = true;
            if (Chart.IsMouseCaptured) Chart.ReleaseMouseCapture();
            Chart.Refresh();
        }
    }
}
