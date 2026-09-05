using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        // Drawing button visual state
        private bool _drawingButtonVisualFixAttached;
        private static readonly bool _drawingButtonVisualFixRegistered = RegisterDrawingButtonVisualFix();

        private static bool RegisterDrawingButtonVisualFix()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(DrawingButtonVisualFix_Loaded));
            return true;
        }

        private static void DrawingButtonVisualFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.AttachDrawingButtonVisualFix();
        }

        private void AttachDrawingButtonVisualFix()
        {
            if (_drawingButtonVisualFixAttached)
                return;

            _drawingButtonVisualFixAttached = true;

            AttachDrawingButton(DrawingSelectButton);
            AttachDrawingButton(DrawingTrendLineButton);
            AttachDrawingButton(DrawingHorizontalLineButton);
            AttachDrawingButton(DrawingVerticalLineButton);
            AttachDrawingButton(DrawingRayButton);
            AttachDrawingButton(DrawingParallelChannelButton);
            AttachDrawingButton(DrawingRectangleButton);
            AttachDrawingButton(DrawingPitchforkButton);
            AttachDrawingButton(DrawingFibRetracementButton);
            AttachDrawingButton(DrawingFibExtensionButton);
            AttachDrawingButton(DrawingTextButton);

            AddHandler(Keyboard.PreviewKeyDownEvent,
                new System.Windows.Input.KeyEventHandler(DrawingButtonVisualFix_KeyDown), true);
            AddHandler(UIElement.PreviewMouseRightButtonDownEvent,
                new System.Windows.Input.MouseButtonEventHandler(DrawingButtonVisualFix_RightMouseDown), true);

            Dispatcher.BeginInvoke(new Action(SetAllDrawingButtonVisuals), DispatcherPriority.ContextIdle);
        }

        private void AttachDrawingButton(System.Windows.Controls.Button button)
        {
            button.AddHandler(
                System.Windows.Controls.Button.ClickEvent,
                new RoutedEventHandler(DrawingButtonVisualFix_Click),
                true);
        }

        private void DrawingButtonVisualFix_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(ApplyDrawingButtonVisualFix), DispatcherPriority.ContextIdle);
        }

        private void ApplyDrawingButtonVisualFix()
        {
            SetAllDrawingButtonVisuals();
        }

        private void DrawingButtonVisualFix_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Escape && e.Key != Key.Cancel)
                return;

            Dispatcher.BeginInvoke(new Action(ApplyDrawingButtonVisualFix), DispatcherPriority.ContextIdle);
        }

        private void DrawingButtonVisualFix_RightMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right)
                return;

            if (_activeDrawingTool != TechnicalDrawingTool.Select || _textDrawingActive)
            {
                Dispatcher.BeginInvoke(new Action(ApplyDrawingButtonVisualFix), DispatcherPriority.ContextIdle);
            }
        }

        // Drawing cancellation and arrow cleanup
        private bool _drawingCancelAndArrowFixAttached;
        private static readonly bool _drawingCancelAndArrowFixRegistered = RegisterDrawingCancelAndArrowFix();

        private static bool RegisterDrawingCancelAndArrowFix()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(DrawingCancelAndArrowFix_Loaded));
            return true;
        }

        private static void DrawingCancelAndArrowFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.AttachDrawingCancelAndArrowFix();
        }

        private void AttachDrawingCancelAndArrowFix()
        {
            if (_drawingCancelAndArrowFixAttached) return;
            _drawingCancelAndArrowFixAttached = true;

            Chart.AddHandler(
                UIElement.PreviewMouseRightButtonDownEvent,
                new MouseButtonEventHandler(DrawingCancelAndArrowFix_RightMouseDown),
                true);
        }

        private void DrawingCancelAndArrowFix_RightMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right)
                return;

            RemoveUnifiedFibPreview();
            _unifiedFibP1 = null;
            _unifiedFibP2 = null;
            RemoveAdvancedPreview();
            _advancedDrawingP1 = null;
            _advancedDrawingP2 = null;
            RemoveHorizontalRayPreview();
            _horizontalRayStart = null;
            RemoveTrendLinePreview();
            _trendLineStart = null;
            RemoveArrowPreview();
            _pendingArrowStart = null;
            _arrowDrawingActive = false;
            SetArrowButtonVisual(false);
            _textDrawingActive = false;
            Chart.ReleaseMouseCapture();
            Chart.UserInputProcessor.IsEnabled = true;
            UpdateTechnicalDrawingButtons();
            UpdateFibonacciButtonVisualState();
            Chart.Refresh();
        }

        // Drawing selection behavior
        private bool _drawingSelectionBehaviorFixAttached;
        private bool _selectionMouseMoved;
        private static readonly bool _drawingSelectionBehaviorFixRegistered = RegisterDrawingSelectionBehaviorFix();

        private static bool RegisterDrawingSelectionBehaviorFix()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(DrawingSelectionBehaviorFix_Loaded));
            return true;
        }

        private static void DrawingSelectionBehaviorFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
            {
                chart.AttachDrawingSelectionBehaviorFix();
                chart.Dispatcher.BeginInvoke(new Action(chart.DisableLegacyDrawingSelectionHandlers), DispatcherPriority.Input);
            }
        }

        private void DisableLegacyDrawingSelectionHandlers()
        {
            Chart.PreviewMouseLeftButtonDown -= DrawingSelection_MouseDown;
            Chart.PreviewMouseMove -= DrawingSelection_MouseMove;
            Chart.PreviewMouseLeftButtonUp -= DrawingSelection_MouseUp;
        }

        private void AttachDrawingSelectionBehaviorFix()
        {
            if (_drawingSelectionBehaviorFixAttached) return;
            _drawingSelectionBehaviorFixAttached = true;

            DisableLegacyDrawingSelectionHandlers();

            Chart.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent,
                new System.Windows.Input.MouseButtonEventHandler(DrawingSelectionBehaviorFix_MouseDown), true);
            Chart.AddHandler(UIElement.PreviewMouseMoveEvent,
                new System.Windows.Input.MouseEventHandler(DrawingSelectionBehaviorFix_MouseMove), true);
            Chart.AddHandler(UIElement.PreviewMouseLeftButtonUpEvent,
                new System.Windows.Input.MouseButtonEventHandler(DrawingSelectionBehaviorFix_MouseUp), true);
            Chart.AddHandler(UIElement.PreviewMouseRightButtonDownEvent,
                new System.Windows.Input.MouseButtonEventHandler(DrawingSelectionBehaviorFix_RightMouseDown), true);
        }

        private void DrawingSelectionBehaviorFix_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || _activeDrawingTool != TechnicalDrawingTool.Select)
                return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates chartPoint)) return;

            if (_textSelection != null)
            {
                if (TryGetTextSelectionHandle(chartPoint, out _))
                {
                    BeginTextSelectionDrag(chartPoint);
                    e.Handled = true;
                    return;
                }

                if (IsPointOnSelectedText(chartPoint))
                {
                    BeginTextSelectionDrag(chartPoint);
                    e.Handled = true;
                    return;
                }
            }

            if (TrySelectTextDrawing(chartPoint))
            {
                _selectionDragging = false;
                _activeDrawingHandle = null;
                Chart.UserInputProcessor.IsEnabled = false;
                e.Handled = true;
                Chart.Refresh();
                return;
            }

            if (_selectedDrawing != null && TryGetHandleAtPoint(chartPoint, out DrawingHandleKind handleKind))
            {
                _activeDrawingHandle = handleKind;
                _selectionDragStart = chartPoint;
                _selectionMouseMoved = false;
                _selectionDragging = true;
                Chart.CaptureMouse();
                Chart.UserInputProcessor.IsEnabled = false;
                e.Handled = true;
                return;
            }

            if (TrySelectDrawing(chartPoint))
            {
                _selectionDragging = false;
                _selectionMouseMoved = false;
                Chart.ReleaseMouseCapture();
                Chart.UserInputProcessor.IsEnabled = false;
                e.Handled = true;
                Chart.Refresh();
            }
            else
            {
                ClearTextSelection();
                ClearDrawingSelection();
            }
        }

        private void DrawingSelectionBehaviorFix_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_textSelectionDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                if (TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates textPoint))
                {
                    if (MoveSelectedText(textPoint))
                    {
                        e.Handled = true;
                        Chart.Refresh();
                    }
                }
                return;
            }

            if (!_selectionDragging || _selectedDrawing == null || _activeDrawingHandle == null || e.LeftButton != MouseButtonState.Pressed)
                return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates drawingPoint)) return;

            if (Math.Abs(drawingPoint.X - _selectionDragStart.X) < 1e-15 &&
                Math.Abs(drawingPoint.Y - _selectionDragStart.Y) < 1e-15)
                return;

            _selectionMouseMoved = true;
            DrawingHandleKind handleKind = _activeDrawingHandle.Value;
            if (MoveSelectedHandle(handleKind, drawingPoint))
            {
                _selectionDragStart = drawingPoint;
                _activeDrawingHandle = handleKind;
                e.Handled = true;
                Chart.Refresh();
            }
        }

        private void DrawingSelectionBehaviorFix_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            if (_textSelectionDragging)
            {
                EndTextSelectionDrag();
                e.Handled = true;
                return;
            }

            if (!_selectionDragging) return;
            _selectionDragging = false;
            _selectionMouseMoved = false;
            _activeDrawingHandle = null;
            Chart.ReleaseMouseCapture();
            Chart.UserInputProcessor.IsEnabled = true;
            e.Handled = true;
            Chart.Refresh();
        }

        private void DrawingSelectionBehaviorFix_RightMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right || _activeDrawingTool != TechnicalDrawingTool.Select)
                return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates point)) return;

            if (_textSelection != null && IsPointOnSelectedText(point))
            {
                ShowTextSelectionContextMenu();
                e.Handled = true;
                return;
            }

            if (_selectedDrawing == null || !IsSelectedDrawingAtPoint(point)) return;

            var menu = new ContextMenu();
            var deleteItem = new MenuItem { Header = "حذف" };
            deleteItem.Click += (_, _) => DeleteSelectedDrawing();
            menu.Items.Add(deleteItem);
            menu.IsOpen = true;
            e.Handled = true;
        }

        private bool IsSelectedDrawingAtPoint(ScottPlot.Coordinates point)
        {
            if (_selectedDrawing == null) return false;
            var candidates = GetDrawingCandidates(point);
            foreach (var candidate in candidates)
            {
                if (candidate.Kind == _selectedDrawingKind && ReferenceEquals(candidate.Drawing, _selectedDrawing))
                    return true;
            }
            return false;
        }

        // Drawing handle and selection visuals
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
            foreach (var marker in _drawingSelectionHandles) Chart.Plot.Remove(marker);
            _drawingSelectionHandles.Clear();
            foreach (var overlay in _drawingSelectionOverlays) Chart.Plot.Remove(overlay);
            _drawingSelectionOverlays.Clear();
            _activeDrawingHandle = null;
        }

        private void RenderDrawingSelectionOverlay()
        {
            ClearDrawingSelectionVisuals();
            if (_selectedDrawing == null || !_allDrawingsVisible) return;
            ScottPlot.Color color = ScottPlot.Color.FromHtml(SelectedDrawingColor);
            foreach (var line in GetSelectedDrawingOverlayLines()) { line.LineColor = color; line.LineWidth = SelectedDrawingWidth; _drawingSelectionOverlays.Add(line); }
            foreach (DrawingHandleInfo handle in GetDrawingHandles())
            {
                var marker = Chart.Plot.Add.Marker(handle.Point.X, handle.Point.Y, ScottPlot.MarkerShape.FilledCircle);
                marker.MarkerSize = DrawingHandleSize; marker.MarkerFillColor = color; marker.MarkerLineColor = ScottPlot.Color.FromHtml("#FFFFFF"); marker.LineWidth = 1.5f;
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
                    { var d = (TrendLineDrawing)_selectedDrawing; result.Add(AddSelectionLine(d.X1, d.Y1, d.X2, d.Y2)); break; }
                case DrawingSelectionKind.HorizontalLine:
                    { var d = (HorizontalLineDrawing)_selectedDrawing; result.Add(AddSelectionLine(limits.Left, d.Y, limits.Right, d.Y)); break; }
                case DrawingSelectionKind.VerticalLine:
                    { var d = (VerticalLineDrawing)_selectedDrawing; result.Add(AddSelectionLine(d.X, limits.Bottom, d.X, limits.Top)); break; }
                case DrawingSelectionKind.Ray:
                    { var d = (RayDrawing)_selectedDrawing; GetRayEnd(d.X1, d.Y1, d.X2, d.Y2, limits, out double endX, out double endY); result.Add(AddSelectionLine(d.X1, d.Y1, endX, endY)); break; }
                case DrawingSelectionKind.ParallelChannel:
                    { var d = (ParallelChannelDrawing)_selectedDrawing; result.Add(AddSelectionLine(d.A.X, d.A.Y, d.B.X, d.B.Y)); double dx = d.B.X - d.A.X, dy = d.B.Y - d.A.Y; result.Add(AddSelectionLine(d.C.X, d.C.Y, d.C.X + dx, d.C.Y + dy)); break; }
                case DrawingSelectionKind.Rectangle:
                    { var d = (RectangleDrawing)_selectedDrawing; GetRectangleCorners(d, out var tl, out var tr, out var br, out var bl); result.Add(AddSelectionLine(tl.X, tl.Y, tr.X, tr.Y)); result.Add(AddSelectionLine(tr.X, tr.Y, br.X, br.Y)); result.Add(AddSelectionLine(br.X, br.Y, bl.X, bl.Y)); result.Add(AddSelectionLine(bl.X, bl.Y, tl.X, tl.Y)); break; }
                case DrawingSelectionKind.Pitchfork:
                    { var d = (PitchforkDrawing)_selectedDrawing; var target = Midpoint(d.B, d.C); AddSelectionRay(result, d.A, target, limits); AddSelectionParallelRay(result, d.A, target, d.B, limits); AddSelectionParallelRay(result, d.A, target, d.C, limits); break; }
                case DrawingSelectionKind.Fibonacci:
                    { var d = (FibonacciDrawing)_selectedDrawing; double ab = d.B.Y - d.A.Y; double[] ratios = d.IsExtension ? new[] { 0.0, .382, .618, 1.0, 1.618, 2.618 } : new[] { 0.0, .236, .382, .5, .618, .786, 1.0 }; foreach (double ratio in ratios) { double y = d.IsExtension ? d.C.Y + ab * ratio : d.B.Y - ab * ratio; double left = d.IsExtension ? Math.Min(d.A.X, d.C.X) : Math.Min(d.A.X, d.B.X); double right = d.IsExtension ? Math.Max(d.A.X, d.C.X) : Math.Max(d.A.X, d.B.X); result.Add(AddSelectionLine(left, y, right, y)); } break; }
            }
            return result;
        }

        private ScottPlot.Plottables.Scatter AddSelectionLine(double x1, double y1, double x2, double y2)
        { var line = Chart.Plot.Add.ScatterLine(new[] { x1, x2 }, new[] { y1, y2 }); line.MarkerSize = 0; return line; }

        private void AddSelectionRay(List<ScottPlot.Plottables.Scatter> lines, ScottPlot.Coordinates start, ScottPlot.Coordinates through, ScottPlot.AxisLimits limits)
        { GetRayEnd(start.X, start.Y, through.X, through.Y, limits, out double endX, out double endY); lines.Add(AddSelectionLine(start.X, start.Y, endX, endY)); }

        private void AddSelectionParallelRay(List<ScottPlot.Plottables.Scatter> lines, ScottPlot.Coordinates directionStart, ScottPlot.Coordinates directionThrough, ScottPlot.Coordinates lineStart, ScottPlot.AxisLimits limits)
        {
            double dx = directionThrough.X - directionStart.X, dy = directionThrough.Y - directionStart.Y;
            if (Math.Abs(dx) < 1e-12) { double endY = dy >= 0 ? limits.Top : limits.Bottom; lines.Add(AddSelectionLine(lineStart.X, lineStart.Y, lineStart.X, endY)); return; }
            double endX = dx >= 0 ? limits.Right : limits.Left; double endY2 = lineStart.Y + dy / dx * (endX - lineStart.X); lines.Add(AddSelectionLine(lineStart.X, lineStart.Y, endX, endY2));
        }

        private static void GetRayEnd(double x1, double y1, double x2, double y2, ScottPlot.AxisLimits limits, out double endX, out double endY)
        {
            double dx = x2 - x1, dy = y2 - y1;
            if (Math.Abs(dx) < 1e-12) { endX = x1; endY = dy >= 0 ? limits.Top : limits.Bottom; return; }
            endX = dx >= 0 ? limits.Right : limits.Left; endY = y1 + dy / dx * (endX - x1);
        }

        private List<DrawingHandleInfo> GetDrawingHandles()
        {
            var result = new List<DrawingHandleInfo>();
            if (_selectedDrawing == null) return result;
            switch (_selectedDrawingKind)
            {
                case DrawingSelectionKind.TrendLine:
                    { var d = (TrendLineDrawing)_selectedDrawing; result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.TrendLineA, Point = new(d.X1, d.Y1) }); result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.TrendLineB, Point = new(d.X2, d.Y2) }); break; }
                case DrawingSelectionKind.HorizontalLine:
                    { var d = (HorizontalLineDrawing)_selectedDrawing; var limits = Chart.Plot.Axes.GetLimits(); result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.HorizontalLine, Point = new((limits.Left + limits.Right) / 2.0, d.Y) }); break; }
                case DrawingSelectionKind.VerticalLine:
                    { var d = (VerticalLineDrawing)_selectedDrawing; var limits = Chart.Plot.Axes.GetLimits(); result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.VerticalLine, Point = new(d.X, (limits.Bottom + limits.Top) / 2.0) }); break; }
                case DrawingSelectionKind.Ray:
                    { var d = (RayDrawing)_selectedDrawing; result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.RayA, Point = new(d.X1, d.Y1) }); break; }
                case DrawingSelectionKind.ParallelChannel:
                    { var d = (ParallelChannelDrawing)_selectedDrawing; result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.ParallelA, Point = d.A }); result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.ParallelB, Point = d.B }); result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.ParallelC, Point = d.C }); break; }
                case DrawingSelectionKind.Rectangle:
                    { var d = (RectangleDrawing)_selectedDrawing; GetRectangleCorners(d, out var tl, out var tr, out var br, out var bl); result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.RectangleTopLeft, Point = tl }); result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.RectangleTopRight, Point = tr }); result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.RectangleBottomRight, Point = br }); result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.RectangleBottomLeft, Point = bl }); break; }
                case DrawingSelectionKind.Pitchfork:
                    { var d = (PitchforkDrawing)_selectedDrawing; result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.PitchforkA, Point = d.A }); result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.PitchforkB, Point = d.B }); result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.PitchforkC, Point = d.C }); break; }
                case DrawingSelectionKind.Fibonacci:
                    { var d = (FibonacciDrawing)_selectedDrawing; result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.FibonacciA, Point = d.A }); result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.FibonacciB, Point = d.B }); if (d.IsExtension) result.Add(new DrawingHandleInfo { Kind = DrawingHandleKind.FibonacciC, Point = d.C }); break; }
            }
            return result;
        }

        private static void GetRectangleCorners(RectangleDrawing d, out ScottPlot.Coordinates topLeft, out ScottPlot.Coordinates topRight, out ScottPlot.Coordinates bottomRight, out ScottPlot.Coordinates bottomLeft)
        {
            double left = Math.Min(d.A.X, d.B.X), right = Math.Max(d.A.X, d.B.X), bottom = Math.Min(d.A.Y, d.B.Y), top = Math.Max(d.A.Y, d.B.Y);
            topLeft = new(left, top); topRight = new(right, top); bottomRight = new(right, bottom); bottomLeft = new(left, bottom);
        }

        private bool TryGetHandleAtPoint(ScottPlot.Coordinates point, out DrawingHandleKind handleKind)
        {
            handleKind = default; if (_selectedDrawing == null) return false;
            double tolerance = GetHandleHitTolerance(); DrawingHandleInfo? nearest = null; double nearestDistance = double.MaxValue; ScottPlot.Pixel mousePixel = Chart.Plot.GetPixel(point);
            foreach (DrawingHandleInfo handle in GetDrawingHandles()) { ScottPlot.Pixel hp = Chart.Plot.GetPixel(handle.Point); double dx = mousePixel.X - hp.X, dy = mousePixel.Y - hp.Y; double distance = Math.Sqrt(dx * dx + dy * dy); if (distance <= tolerance && distance < nearestDistance) { nearest = handle; nearestDistance = distance; } }
            if (nearest == null) return false; handleKind = nearest.Kind; return true;
        }

        private double GetHandleHitTolerance() => 11.0;

        private bool MoveSelectedHandle(DrawingHandleKind handleKind, ScottPlot.Coordinates point)
        {
            if (_selectedDrawing == null) return false;
            switch (_selectedDrawingKind)
            {
                case DrawingSelectionKind.TrendLine:
                    { var d = (TrendLineDrawing)_selectedDrawing; RemovePlotLine(d.PlotLine); if (handleKind == DrawingHandleKind.TrendLineA) { d.X1 = point.X; d.Y1 = point.Y; } else { d.X2 = point.X; d.Y2 = point.Y; } AddTrendLineToChart(d); break; }
                case DrawingSelectionKind.HorizontalLine:
                    { var d = (HorizontalLineDrawing)_selectedDrawing; RemovePlotLine(d.PlotLine); d.Y = point.Y; AddHorizontalLineToChart(d); break; }
                case DrawingSelectionKind.VerticalLine:
                    { var d = (VerticalLineDrawing)_selectedDrawing; RemovePlotLine(d.PlotLine); d.X = point.X; AddVerticalLineToChart(d); break; }
                case DrawingSelectionKind.Ray:
                    { var d = (RayDrawing)_selectedDrawing; if (handleKind != DrawingHandleKind.RayA) return false; RemovePlotLine(d.PlotLine); d.X1 = point.X; d.Y1 = point.Y; d.X2 = point.X + 1.0; d.Y2 = point.Y; AddRayToChart(d); break; }
                case DrawingSelectionKind.ParallelChannel:
                    { var d = (ParallelChannelDrawing)_selectedDrawing; RemovePlotLine(d.BaseLine); RemovePlotLine(d.ParallelLine); if (handleKind == DrawingHandleKind.ParallelA) d.A = point; else if (handleKind == DrawingHandleKind.ParallelB) d.B = point; else d.C = point; AddParallelChannelToChart(d); break; }
                case DrawingSelectionKind.Rectangle:
                    { var d = (RectangleDrawing)_selectedDrawing; GetRectangleCorners(d, out var tl, out var tr, out var br, out var bl); ScottPlot.Coordinates opposite; switch (handleKind) { case DrawingHandleKind.RectangleTopLeft: opposite = br; break; case DrawingHandleKind.RectangleTopRight: opposite = bl; break; case DrawingHandleKind.RectangleBottomRight: opposite = tl; break; default: opposite = tr; break; } RemoveRectangleLines(d); d.A = point; d.B = opposite; AddRectangleToChart(d); break; }
                case DrawingSelectionKind.Pitchfork:
                    { var d = (PitchforkDrawing)_selectedDrawing; RemovePitchforkLines(d); if (handleKind == DrawingHandleKind.PitchforkA) d.A = point; else if (handleKind == DrawingHandleKind.PitchforkB) d.B = point; else d.C = point; AddPitchforkToChart(d); break; }
                case DrawingSelectionKind.Fibonacci:
                    { var d = (FibonacciDrawing)_selectedDrawing; RemoveFibonacciLines(d); if (handleKind == DrawingHandleKind.FibonacciA) d.A = point; else if (handleKind == DrawingHandleKind.FibonacciB) d.B = point; else d.C = point; RenderFibonacciDrawing(d); break; }
                default: return false;
            }
            RenderDrawingSelectionOverlay(); return true;
        }

        private void DeleteSelectedDrawingAndClearVisuals() => DeleteSelectedDrawing();
    }
}
