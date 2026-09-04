using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private const int ArrowDrawingToolValue = 10;
        private const double ArrowHitTolerancePixels = 10.0;
        private const double ArrowHandleHitTolerancePixels = 12.0;

        private sealed class ArrowDrawing
        {
            public double X1 { get; set; }
            public double Y1 { get; set; }
            public double X2 { get; set; }
            public double Y2 { get; set; }
            public ScottPlot.Plottables.Arrow? PlotArrow { get; set; }
        }

        private readonly System.Collections.Generic.List<ArrowDrawing> _arrowDrawings = new();
        private bool _arrowDrawingActive;
        private bool _arrowEventsAttached;
        private bool _arrowSelectionAttached;
        private bool _arrowsVisible = true;
        private ArrowDrawing? _selectedArrow;
        private int _selectedArrowHandle;
        private bool _arrowSelectionDragging;
        private ScottPlot.Coordinates? _pendingArrowStart;
        private ScottPlot.Plottables.Scatter? _arrowPreview;
        private ScottPlot.Plottables.Scatter? _selectedArrowOverlay;
        private ScottPlot.Plottables.Marker? _selectedArrowHandle1;
        private ScottPlot.Plottables.Marker? _selectedArrowHandle2;

        private static readonly bool _arrowInitialViewRegistered = RegisterArrowInitialViewHandling();

        private static bool RegisterArrowInitialViewHandling()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(ArrowInitialView_Loaded));
            return true;
        }

        private static void ArrowInitialView_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart) return;
            chart.AttachArrowHandling();
        }

        private void AttachArrowHandling()
        {
            if (_arrowEventsAttached) return;
            _arrowEventsAttached = true;
            DrawingArrowButton.Click += DrawingArrowButton_Click;
            Chart.PreviewMouseLeftButtonDown += ArrowDrawing_MouseDown;
            Chart.PreviewMouseMove += ArrowDrawing_MouseMove;
            Chart.PreviewMouseLeftButtonUp += ArrowDrawing_MouseUp;
            DrawingSelectButton.Click += ArrowDeactivateFromOtherTool;
            DrawingTrendLineButton.Click += ArrowDeactivateFromOtherTool;
            DrawingHorizontalLineButton.Click += ArrowDeactivateFromOtherTool;
            DrawingVerticalLineButton.Click += ArrowDeactivateFromOtherTool;
            DrawingRayButton.Click += ArrowDeactivateFromOtherTool;
            DrawingParallelChannelButton.Click += ArrowDeactivateFromOtherTool;
            DrawingRectangleButton.Click += ArrowDeactivateFromOtherTool;
            DrawingPitchforkButton.Click += ArrowDeactivateFromOtherTool;
            DrawingFibRetracementButton.Click += ArrowDeactivateFromOtherTool;
            DrawingFibExtensionButton.Click += ArrowDeactivateFromOtherTool;
            DrawingTextButton.Click += ArrowDeactivateFromOtherTool;
            DrawingArrowButton.AddHandler(UIElement.PreviewMouseRightButtonDownEvent,
                new MouseButtonEventHandler(ArrowSettings_RightMouseDown), true);
            HideAllDrawingsButton.Click += ArrowHideAll_Click;
            DeleteAllDrawingsButton.Click += ArrowDeleteAll_Click;
            AttachArrowSelectionHandling();
            Chart.Plot.RenderManager.RenderStarting += ArrowRenderStarting;
        }

        private void AttachArrowSelectionHandling()
        {
            if (_arrowSelectionAttached) return;
            _arrowSelectionAttached = true;
            Chart.PreviewMouseLeftButtonDown += ArrowSelection_MouseDown;
            Chart.PreviewMouseMove += ArrowSelection_MouseMove;
            Chart.PreviewMouseLeftButtonUp += ArrowSelection_MouseUp;
            Chart.PreviewMouseRightButtonDown += ArrowSelection_RightMouseDown;
            AddHandler(Keyboard.PreviewKeyDownEvent,
                new System.Windows.Input.KeyEventHandler(ArrowSelection_KeyDown), true);
        }

        private void DrawingArrowButton_Click(object? sender, RoutedEventArgs e)
        {
            CancelArrowDrawing(false);
            ClearArrowSelection();
            _arrowsVisible = true;
            _arrowDrawingActive = true;
            _activeDrawingTool = (TechnicalDrawingTool)ArrowDrawingToolValue;
            _textDrawingActive = false;
            Chart.UserInputProcessor.IsEnabled = false;
            Chart.Focusable = true;
            Chart.Focus();
            SetArrowButtonVisual(true);
            UpdateTechnicalDrawingButtons();
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | پیکان: نقطه شروع را کلیک کنید";
            Chart.Refresh();
        }

        private void ArrowDeactivateFromOtherTool(object? sender, RoutedEventArgs e)
        {
            if (ReferenceEquals(sender, DrawingArrowButton)) return;
            CancelArrowDrawing(false);
            SetArrowButtonVisual(false);
        }

        private void SetArrowButtonVisual(bool selected)
        {
            DrawingArrowButton.Background = selected
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(25, 118, 210))
                : Brushes.Transparent;
            DrawingArrowButton.Foreground = selected ? Brushes.White : Brushes.Black;
            DrawingArrowButton.BorderBrush = selected
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(25, 118, 210))
                : Brushes.Transparent;
        }

        private void ArrowDrawing_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_arrowDrawingActive || (int)_activeDrawingTool != ArrowDrawingToolValue || e.ChangedButton != MouseButton.Left) return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates point)) return;

            if (!_pendingArrowStart.HasValue)
            {
                _pendingArrowStart = point;
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | پیکان: نقطه انتهایی را کلیک کنید";
                e.Handled = true;
                return;
            }

            var start = _pendingArrowStart.Value;
            if (Math.Abs(start.X - point.X) < 1e-12 && Math.Abs(start.Y - point.Y) < 1e-12) return;
            RemoveArrowPreview();
            var drawing = new ArrowDrawing { X1 = start.X, Y1 = start.Y, X2 = point.X, Y2 = point.Y };
            _arrowDrawings.Add(drawing);
            AddArrowToChart(drawing);
            _pendingArrowStart = null;
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | پیکان رسم شد؛ پیکان بعدی را شروع کنید";
            Chart.Refresh();
            e.Handled = true;
        }

        private void ArrowDrawing_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_arrowDrawingActive || (int)_activeDrawingTool != ArrowDrawingToolValue || !_pendingArrowStart.HasValue) return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates point)) return;
            RemoveArrowPreview();
            _arrowPreview = Chart.Plot.Add.ScatterLine(
                new[] { _pendingArrowStart.Value.X, point.X },
                new[] { _pendingArrowStart.Value.Y, point.Y });
            var style = GetDrawingToolStyle("Arrow");
            _arrowPreview.MarkerSize = 0;
            _arrowPreview.LineColor = ScottPlot.Color.FromHtml(style.Color);
            _arrowPreview.LineWidth = (float)Math.Max(0.5, style.LineWidth);
            _arrowPreview.LinePattern = GetDrawingLinePattern(style.LineStyle);
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | پیکان: نقطه انتهایی را انتخاب کنید | قیمت: {point.Y:N2}";
            Chart.Refresh();
        }

        private void ArrowDrawing_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) { }

        private void ArrowSettings_RightMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right) return;
            ShowDrawingToolSettings("Arrow", "پیکان");
            e.Handled = true;
        }

        private void AddArrowToChart(ArrowDrawing drawing)
        {
            var style = GetDrawingToolStyle("Arrow");
            var arrow = Chart.Plot.Add.Arrow(
                new ScottPlot.Coordinates(drawing.X1, drawing.Y1),
                new ScottPlot.Coordinates(drawing.X2, drawing.Y2));
            arrow.ArrowLineColor = ScottPlot.Color.FromHtml(style.Color);
            arrow.ArrowFillColor = ScottPlot.Color.FromHtml(style.Color);
            arrow.ArrowLineWidth = (float)Math.Max(0.5, style.LineWidth);
            arrow.ArrowWidth = (float)Math.Max(1.0, style.LineWidth * 2.5);
            arrow.ArrowheadLength = 12;
            arrow.ArrowheadAxisLength = 12;
            arrow.ArrowheadWidth = 8;
            arrow.ArrowStyle.LineStyle.Pattern = style.LineStyle == "Dash"
                ? ScottPlot.LinePattern.Dashed
                : style.LineStyle == "Dot"
                    ? ScottPlot.LinePattern.Dotted
                    : ScottPlot.LinePattern.Solid;
            arrow.IsVisible = _arrowsVisible && _allDrawingsVisible;
            drawing.PlotArrow = arrow;
        }

        private void ArrowRenderStarting(object? sender, ScottPlot.RenderPack e)
        {
            if (!_arrowsVisible || !_allDrawingsVisible) return;
            foreach (var drawing in _arrowDrawings)
            {
                if (drawing.PlotArrow == null || !Chart.Plot.GetPlottables().Contains(drawing.PlotArrow))
                    AddArrowToChart(drawing);
            }
        }

        private void RenderArrowDrawings()
        {
            if (!_arrowsVisible || !_allDrawingsVisible) return;
            foreach (var drawing in _arrowDrawings)
            {
                if (drawing.PlotArrow != null) Chart.Plot.Remove(drawing.PlotArrow);
                AddArrowToChart(drawing);
            }
        }

        private void ArrowSelection_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || _arrowDrawingActive || (int)_activeDrawingTool != (int)TechnicalDrawingTool.Select)
                return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates point)) return;

            if (_selectedArrow != null)
            {
                int handle = HitTestArrowHandle(_selectedArrow, point);
                if (handle != 0)
                {
                    _selectedArrowHandle = handle;
                    _arrowSelectionDragging = true;
                    Chart.CaptureMouse();
                    Chart.UserInputProcessor.IsEnabled = false;
                    e.Handled = true;
                    return;
                }
            }

            ArrowDrawing? hit = HitTestArrow(point);
            if (hit != null)
            {
                SelectArrow(hit);
                e.Handled = true;
                Chart.UserInputProcessor.IsEnabled = false;
                Chart.Refresh();
            }
            else
            {
                ClearArrowSelection();
            }
        }

        private void ArrowSelection_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_arrowSelectionDragging || _selectedArrow == null || _selectedArrowHandle == 0 || e.LeftButton != MouseButtonState.Pressed)
                return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates point)) return;

            RemoveArrowPlot(_selectedArrow);
            if (_selectedArrowHandle == 1)
            {
                _selectedArrow.X1 = point.X;
                _selectedArrow.Y1 = point.Y;
            }
            else
            {
                _selectedArrow.X2 = point.X;
                _selectedArrow.Y2 = point.Y;
            }
            AddArrowToChart(_selectedArrow);
            RenderSelectedArrowOverlay();
            e.Handled = true;
            Chart.Refresh();
        }

        private void ArrowSelection_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || !_arrowSelectionDragging) return;
            _arrowSelectionDragging = false;
            _selectedArrowHandle = 0;
            Chart.ReleaseMouseCapture();
            Chart.UserInputProcessor.IsEnabled = true;
            e.Handled = true;
            Chart.Refresh();
        }

        private void ArrowSelection_RightMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right || _arrowDrawingActive || (int)_activeDrawingTool != (int)TechnicalDrawingTool.Select)
                return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates point)) return;
            ArrowDrawing? hit = HitTestArrow(point);
            if (hit == null) return;
            SelectArrow(hit);
            var menu = new ContextMenu();
            var delete = new MenuItem { Header = "حذف" };
            delete.Click += (_, _) => DeleteSelectedArrow();
            menu.Items.Add(delete);
            menu.IsOpen = true;
            e.Handled = true;
        }

        private void ArrowSelection_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Delete && e.Key != Key.Back) return;
            if (_selectedArrow == null || _arrowDrawingActive || (int)_activeDrawingTool != (int)TechnicalDrawingTool.Select) return;
            DeleteSelectedArrow();
            e.Handled = true;
        }

        private ArrowDrawing? HitTestArrow(ScottPlot.Coordinates point)
        {
            if (!_arrowsVisible || !_allDrawingsVisible) return null;
            ScottPlot.Pixel mouse = Chart.Plot.GetPixel(point);
            ArrowDrawing? best = null;
            double bestDistance = double.MaxValue;
            for (int i = _arrowDrawings.Count - 1; i >= 0; i--)
            {
                ArrowDrawing drawing = _arrowDrawings[i];
                double distance = DistancePixelToSegment(mouse,
                    Chart.Plot.GetPixel(new ScottPlot.Coordinates(drawing.X1, drawing.Y1)),
                    Chart.Plot.GetPixel(new ScottPlot.Coordinates(drawing.X2, drawing.Y2)));
                if (distance <= ArrowHitTolerancePixels && distance < bestDistance)
                {
                    best = drawing;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private int HitTestArrowHandle(ArrowDrawing drawing, ScottPlot.Coordinates point)
        {
            ScottPlot.Pixel mouse = Chart.Plot.GetPixel(point);
            ScottPlot.Pixel p1 = Chart.Plot.GetPixel(new ScottPlot.Coordinates(drawing.X1, drawing.Y1));
            ScottPlot.Pixel p2 = Chart.Plot.GetPixel(new ScottPlot.Coordinates(drawing.X2, drawing.Y2));
            double d1 = PixelDistance(mouse, p1);
            double d2 = PixelDistance(mouse, p2);
            if (d1 <= ArrowHandleHitTolerancePixels && d1 <= d2) return 1;
            if (d2 <= ArrowHandleHitTolerancePixels) return 2;
            return 0;
        }

        private static double PixelDistance(ScottPlot.Pixel a, ScottPlot.Pixel b)
        {
            double dx = a.X - b.X, dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static double DistancePixelToSegment(ScottPlot.Pixel p, ScottPlot.Pixel a, ScottPlot.Pixel b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y;
            double length2 = dx * dx + dy * dy;
            if (length2 < 1e-12) return PixelDistance(p, a);
            double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / length2;
            t = Math.Max(0, Math.Min(1, t));
            return PixelDistance(p, new ScottPlot.Pixel(a.X + t * dx, a.Y + t * dy));
        }

        private void SelectArrow(ArrowDrawing drawing)
        {
            _selectedArrow = drawing;
            RenderSelectedArrowOverlay();
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | پیکان انتخاب شد؛ نقاط کنترل را جابه‌جا کنید | حذف: Delete";
        }

        private void RenderSelectedArrowOverlay()
        {
            ClearArrowSelectionVisuals();
            if (_selectedArrow == null || !_arrowsVisible || !_allDrawingsVisible) return;
            var color = ScottPlot.Color.FromHtml("#1976D2");
            _selectedArrowOverlay = Chart.Plot.Add.ScatterLine(
                new[] { _selectedArrow.X1, _selectedArrow.X2 },
                new[] { _selectedArrow.Y1, _selectedArrow.Y2 });
            _selectedArrowOverlay.MarkerSize = 0;
            _selectedArrowOverlay.LineColor = color;
            _selectedArrowOverlay.LineWidth = 3;
            _selectedArrowHandle1 = AddArrowHandle(_selectedArrow.X1, _selectedArrow.Y1, color);
            _selectedArrowHandle2 = AddArrowHandle(_selectedArrow.X2, _selectedArrow.Y2, color);
        }

        private ScottPlot.Plottables.Marker AddArrowHandle(double x, double y, ScottPlot.Color color)
        {
            var marker = Chart.Plot.Add.Marker(x, y, ScottPlot.MarkerShape.FilledCircle);
            marker.MarkerSize = 12;
            marker.MarkerFillColor = color;
            marker.MarkerLineColor = ScottPlot.Colors.White;
            marker.LineWidth = 1.5f;
            return marker;
        }

        private void ClearArrowSelectionVisuals()
        {
            if (_selectedArrowOverlay != null) Chart.Plot.Remove(_selectedArrowOverlay);
            if (_selectedArrowHandle1 != null) Chart.Plot.Remove(_selectedArrowHandle1);
            if (_selectedArrowHandle2 != null) Chart.Plot.Remove(_selectedArrowHandle2);
            _selectedArrowOverlay = null;
            _selectedArrowHandle1 = null;
            _selectedArrowHandle2 = null;
        }

        private void ClearArrowSelection()
        {
            ClearArrowSelectionVisuals();
            _selectedArrow = null;
            _selectedArrowHandle = 0;
        }

        private void RemoveArrowPlot(ArrowDrawing drawing)
        {
            if (drawing.PlotArrow != null)
            {
                Chart.Plot.Remove(drawing.PlotArrow);
                drawing.PlotArrow = null;
            }
        }

        private void DeleteSelectedArrow()
        {
            if (_selectedArrow == null) return;
            RemoveArrowPlot(_selectedArrow);
            _arrowDrawings.Remove(_selectedArrow);
            ClearArrowSelection();
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | پیکان حذف شد";
            Chart.Refresh();
        }

        private void RemoveArrowPreview()
        {
            if (_arrowPreview != null) Chart.Plot.Remove(_arrowPreview);
            _arrowPreview = null;
        }

        private void CancelArrowDrawing(bool refresh = true)
        {
            RemoveArrowPreview();
            _pendingArrowStart = null;
            _arrowDrawingActive = false;
            if ((int)_activeDrawingTool == ArrowDrawingToolValue)
            {
                _activeDrawingTool = TechnicalDrawingTool.Select;
                Chart.UserInputProcessor.IsEnabled = true;
            }
            SetArrowButtonVisual(false);
            if (refresh) Chart.Refresh();
        }

        private void ArrowHideAll_Click(object? sender, RoutedEventArgs e)
        {
            _arrowsVisible = _allDrawingsVisible;
            foreach (var drawing in _arrowDrawings)
                if (drawing.PlotArrow != null) drawing.PlotArrow.IsVisible = _arrowsVisible;
            if (!_arrowsVisible) ClearArrowSelection();
            RemoveArrowPreview();
            Chart.Refresh();
        }

        private void ArrowDeleteAll_Click(object? sender, RoutedEventArgs e)
        {
            foreach (var drawing in _arrowDrawings)
                RemoveArrowPlot(drawing);
            _arrowDrawings.Clear();
            ClearArrowSelection();
            RemoveArrowPreview();
            Chart.Refresh();
        }
    }
}
