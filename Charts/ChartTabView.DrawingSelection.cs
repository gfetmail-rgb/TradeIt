using System;
using System.Windows;
using System.Windows.Input;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private enum DrawingSelectionKind
        {
            None, TrendLine, HorizontalLine, VerticalLine, Ray, ParallelChannel, Rectangle, Pitchfork
        }

        private DrawingSelectionKind _selectedDrawingKind;
        private object? _selectedDrawing;
        private ScottPlot.Coordinates _selectionDragStart;
        private bool _selectionDragging;
        private bool _drawingSelectionAttached;

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
            if (!TryGetRawChartPoint(e, out ScottPlot.Coordinates point)) return;

            if (!TrySelectDrawing(point))
            {
                ClearDrawingSelection();
                return;
            }

            _selectionDragStart = point;
            _selectionDragging = true;
            Chart.CaptureMouse();
            Chart.UserInputProcessor.IsEnabled = false;
            e.Handled = true;
            Chart.Refresh();
        }

        private void DrawingSelection_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_selectionDragging || _selectedDrawing == null || e.LeftButton != MouseButtonState.Pressed) return;
            if (!TryGetRawChartPoint(e, out ScottPlot.Coordinates point)) return;

            double dx = point.X - _selectionDragStart.X;
            double dy = point.Y - _selectionDragStart.Y;
            if (Math.Abs(dx) < 1e-15 && Math.Abs(dy) < 1e-15) return;

            MoveSelectedDrawing(dx, dy);
            _selectionDragStart = point;
            e.Handled = true;
            Chart.Refresh();
        }

        private void DrawingSelection_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || !_selectionDragging) return;
            _selectionDragging = false;
            Chart.ReleaseMouseCapture();
            Chart.UserInputProcessor.IsEnabled = true;
            e.Handled = true;
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
            ClearDrawingSelection();
            double tolerance = GetDrawingHitTolerance();

            for (int i = _pitchforks.Count - 1; i >= 0; i--)
            {
                var d = _pitchforks[i];
                if (DistanceToPitchfork(point, d) <= tolerance) return SelectDrawing(DrawingSelectionKind.Pitchfork, d);
            }
            for (int i = _parallelChannels.Count - 1; i >= 0; i--)
            {
                var d = _parallelChannels[i];
                if (DistancePointToSegment(point, d.A, d.B) <= tolerance ||
                    DistancePointToSegment(point, d.C, new ScottPlot.Coordinates(d.C.X + d.B.X - d.A.X, d.C.Y + d.B.Y - d.A.Y)) <= tolerance)
                    return SelectDrawing(DrawingSelectionKind.ParallelChannel, d);
            }
            for (int i = _drawingRectangles.Count - 1; i >= 0; i--)
            {
                var d = _drawingRectangles[i];
                double left = Math.Min(d.A.X, d.B.X), right = Math.Max(d.A.X, d.B.X);
                double bottom = Math.Min(d.A.Y, d.B.Y), top = Math.Max(d.A.Y, d.B.Y);
                if (DistancePointToSegment(point, new ScottPlot.Coordinates(left, bottom), new ScottPlot.Coordinates(right, bottom)) <= tolerance ||
                    DistancePointToSegment(point, new ScottPlot.Coordinates(right, bottom), new ScottPlot.Coordinates(right, top)) <= tolerance ||
                    DistancePointToSegment(point, new ScottPlot.Coordinates(right, top), new ScottPlot.Coordinates(left, top)) <= tolerance ||
                    DistancePointToSegment(point, new ScottPlot.Coordinates(left, top), new ScottPlot.Coordinates(left, bottom)) <= tolerance)
                    return SelectDrawing(DrawingSelectionKind.Rectangle, d);
            }
            for (int i = _rays.Count - 1; i >= 0; i--)
            {
                var d = _rays[i];
                if (DistanceToRay(point, d.X1, d.Y1, d.X2, d.Y2) <= tolerance) return SelectDrawing(DrawingSelectionKind.Ray, d);
            }
            for (int i = _trendLines.Count - 1; i >= 0; i--)
            {
                var d = _trendLines[i];
                if (DistancePointToSegment(point, new ScottPlot.Coordinates(d.X1, d.Y1), new ScottPlot.Coordinates(d.X2, d.Y2)) <= tolerance)
                    return SelectDrawing(DrawingSelectionKind.TrendLine, d);
            }
            for (int i = _horizontalLines.Count - 1; i >= 0; i--)
            {
                if (Math.Abs(point.Y - _horizontalLines[i].Y) <= tolerance) return SelectDrawing(DrawingSelectionKind.HorizontalLine, _horizontalLines[i]);
            }
            for (int i = _verticalLines.Count - 1; i >= 0; i--)
            {
                if (Math.Abs(point.X - _verticalLines[i].X) <= tolerance) return SelectDrawing(DrawingSelectionKind.VerticalLine, _verticalLines[i]);
            }
            return false;
        }

        private bool SelectDrawing(DrawingSelectionKind kind, object drawing)
        {
            _selectedDrawingKind = kind;
            _selectedDrawing = drawing;
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | ابزار انتخاب شد؛ برای جابجایی Drag کنید | حذف: Delete";
            return true;
        }

        private double GetDrawingHitTolerance()
        {
            var limits = Chart.Plot.Axes.GetLimits();
            double width = Math.Max(1.0, Chart.ActualWidth);
            double height = Math.Max(1.0, Chart.ActualHeight);
            double tx = Math.Abs(limits.Right - limits.Left) * 8.0 / width;
            double ty = Math.Abs(limits.Top - limits.Bottom) * 8.0 / height;
            return Math.Max(tx, ty);
        }

        private static double DistancePointToSegment(ScottPlot.Coordinates p, ScottPlot.Coordinates a, ScottPlot.Coordinates b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y;
            double length2 = dx * dx + dy * dy;
            if (length2 < 1e-24) return Math.Sqrt((p.X - a.X) * (p.X - a.X) + (p.Y - a.Y) * (p.Y - a.Y));
            double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / length2;
            t = Math.Max(0, Math.Min(1, t));
            double x = a.X + t * dx, y = a.Y + t * dy;
            return Math.Sqrt((p.X - x) * (p.X - x) + (p.Y - y) * (p.Y - y));
        }

        private double DistanceToRay(ScottPlot.Coordinates p, double x1, double y1, double x2, double y2)
        {
            var limits = Chart.Plot.Axes.GetLimits();
            double dx = x2 - x1, dy = y2 - y1;
            if (Math.Abs(dx) < 1e-12)
                return DistancePointToSegment(p, new ScottPlot.Coordinates(x1, y1), new ScottPlot.Coordinates(x1, dy >= 0 ? limits.Top : limits.Bottom));
            double endX = dx >= 0 ? limits.Right : limits.Left;
            double endY = y1 + dy / dx * (endX - x1);
            return DistancePointToSegment(p, new ScottPlot.Coordinates(x1, y1), new ScottPlot.Coordinates(endX, endY));
        }

        private double DistanceToPitchfork(ScottPlot.Coordinates p, PitchforkDrawing d)
        {
            var target = Midpoint(d.B, d.C);
            double median = DistanceToRay(p, d.A.X, d.A.Y, target.X, target.Y);
            double upper = DistanceToRay(p, d.B.X, d.B.Y, target.X, target.Y);
            double lower = DistanceToRay(p, d.C.X, d.C.Y, target.X, target.Y);
            return Math.Min(median, Math.Min(upper, lower));
        }

        private void MoveSelectedDrawing(double dx, double dy)
        {
            switch (_selectedDrawingKind)
            {
                case DrawingSelectionKind.HorizontalLine:
                    {
                        var old = (HorizontalLineDrawing)_selectedDrawing!;
                        int index = _horizontalLines.IndexOf(old);
                        if (index < 0) return;
                        RemovePlotLine(old.PlotLine);
                        var replacement = new HorizontalLineDrawing { Y = old.Y + dy };
                        _horizontalLines[index] = replacement;
                        _selectedDrawing = replacement;
                        AddHorizontalLineToChart(replacement);
                        break;
                    }
                case DrawingSelectionKind.VerticalLine:
                    {
                        var old = (VerticalLineDrawing)_selectedDrawing!;
                        int index = _verticalLines.IndexOf(old);
                        if (index < 0) return;
                        RemovePlotLine(old.PlotLine);
                        var replacement = new VerticalLineDrawing { X = old.X + dx };
                        _verticalLines[index] = replacement;
                        _selectedDrawing = replacement;
                        AddVerticalLineToChart(replacement);
                        break;
                    }
                case DrawingSelectionKind.TrendLine:
                    {
                        var old = (TrendLineDrawing)_selectedDrawing!;
                        int index = _trendLines.IndexOf(old);
                        if (index < 0) return;
                        RemovePlotLine(old.PlotLine);
                        var replacement = new TrendLineDrawing { X1 = old.X1 + dx, Y1 = old.Y1 + dy, X2 = old.X2 + dx, Y2 = old.Y2 + dy };
                        _trendLines[index] = replacement;
                        _selectedDrawing = replacement;
                        AddTrendLineToChart(replacement);
                        break;
                    }
                case DrawingSelectionKind.Ray:
                    {
                        var old = (RayDrawing)_selectedDrawing!;
                        int index = _rays.IndexOf(old);
                        if (index < 0) return;
                        RemovePlotLine(old.PlotLine);
                        var replacement = new RayDrawing { X1 = old.X1 + dx, Y1 = old.Y1 + dy, X2 = old.X2 + dx, Y2 = old.Y2 + dy };
                        _rays[index] = replacement;
                        _selectedDrawing = replacement;
                        AddRayToChart(replacement);
                        break;
                    }
                case DrawingSelectionKind.ParallelChannel:
                    {
                        var d = (ParallelChannelDrawing)_selectedDrawing!;
                        RemovePlotLine(d.BaseLine);
                        RemovePlotLine(d.ParallelLine);
                        d.A = Offset(d.A, dx, dy); d.B = Offset(d.B, dx, dy); d.C = Offset(d.C, dx, dy);
                        AddParallelChannelToChart(d);
                        break;
                    }
                case DrawingSelectionKind.Rectangle:
                    {
                        var d = (RectangleDrawing)_selectedDrawing!;
                        RemoveRectangleLines(d);
                        d.A = Offset(d.A, dx, dy); d.B = Offset(d.B, dx, dy);
                        AddRectangleToChart(d);
                        break;
                    }
                case DrawingSelectionKind.Pitchfork:
                    {
                        var d = (PitchforkDrawing)_selectedDrawing!;
                        RemovePitchforkLines(d);
                        d.A = Offset(d.A, dx, dy); d.B = Offset(d.B, dx, dy); d.C = Offset(d.C, dx, dy);
                        AddPitchforkToChart(d);
                        break;
                    }
            }
        }

        private static ScottPlot.Coordinates Offset(ScottPlot.Coordinates p, double dx, double dy) => new(p.X + dx, p.Y + dy);

        private void DeleteSelectedDrawing()
        {
            if (_selectedDrawing == null) return;
            switch (_selectedDrawingKind)
            {
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
            _selectedDrawing = null;
            _selectedDrawingKind = DrawingSelectionKind.None;
            _selectionDragging = false;
            Chart.UserInputProcessor.IsEnabled = true;
            if (Chart.IsMouseCaptured) Chart.ReleaseMouseCapture();
        }
    }
}
