using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfKeyEventHandler = System.Windows.Input.KeyEventHandler;
using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfMouseButtonEventHandler = System.Windows.Input.MouseButtonEventHandler;
using WpfPoint = System.Windows.Point;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private enum TechnicalDrawingTool { Select, TrendLine, HorizontalLine, VerticalLine, Ray }
        private sealed class TrendLineDrawing { public double X1 { get; set; } public double Y1 { get; set; } public double X2 { get; set; } public double Y2 { get; set; } public ScottPlot.Plottables.Scatter? PlotLine { get; set; } }
        private sealed class HorizontalLineDrawing { public double Y { get; set; } public ScottPlot.Plottables.HorizontalLine? PlotLine { get; set; } }
        private sealed class VerticalLineDrawing { public double X { get; set; } public ScottPlot.Plottables.VerticalLine? PlotLine { get; set; } }
        private sealed class RayDrawing { public double X1 { get; set; } public double Y1 { get; set; } public double X2 { get; set; } public double Y2 { get; set; } public ScottPlot.Plottables.Scatter? PlotLine { get; set; } }

        private readonly List<TrendLineDrawing> _trendLines = new();
        private readonly List<HorizontalLineDrawing> _horizontalLines = new();
        private readonly List<VerticalLineDrawing> _verticalLines = new();
        private readonly List<RayDrawing> _rays = new();
        private TechnicalDrawingTool _activeDrawingTool = TechnicalDrawingTool.Select;
        private ScottPlot.Coordinates? _trendLineStart;
        private ScottPlot.Plottables.Scatter? _trendLinePreview;
        private bool _technicalDrawingEventsAttached;
        private bool _suppressContextMenuAfterCancel;

        private void InitializeTechnicalDrawingHandling()
        {
            if (_technicalDrawingEventsAttached) return;
            _technicalDrawingEventsAttached = true;
            Chart.PreviewMouseLeftButtonDown += TechnicalDrawing_MouseDown;
            Chart.PreviewMouseMove += TechnicalDrawing_MouseMove;
            AddHandler(UIElement.PreviewMouseRightButtonDownEvent, new WpfMouseButtonEventHandler(TechnicalDrawing_RightMouseDown), true);
            AddHandler(Keyboard.PreviewKeyDownEvent, new WpfKeyEventHandler(TechnicalDrawing_KeyDown), true);
            Focusable = true;
            ChartTypeComboBox.SelectionChanged += TechnicalDrawing_ChartTypeChanged;
            UpdateTechnicalDrawingButtons();
        }

        private void DrawingSelectButton_Click(object sender, RoutedEventArgs e) => SetTechnicalDrawingTool(TechnicalDrawingTool.Select);
        private void DrawingTrendLineButton_Click(object sender, RoutedEventArgs e) { _textDrawingActive = false; SetTechnicalDrawingTool(TechnicalDrawingTool.TrendLine); FocusChartForDrawing(); }
        private void DrawingHorizontalLineButton_Click(object sender, RoutedEventArgs e) { _textDrawingActive = false; SetTechnicalDrawingTool(TechnicalDrawingTool.HorizontalLine); FocusChartForDrawing(); }
        private void DrawingVerticalLineButton_Click(object sender, RoutedEventArgs e) { _textDrawingActive = false; SetTechnicalDrawingTool(TechnicalDrawingTool.VerticalLine); FocusChartForDrawing(); }
        private void DrawingRayButton_Click(object sender, RoutedEventArgs e) { _textDrawingActive = false; SetTechnicalDrawingTool(TechnicalDrawingTool.Ray); FocusChartForDrawing(); }
        private void FocusChartForDrawing() { Chart.Focusable = true; Chart.Focus(); Focus(); }

        private void SetTechnicalDrawingTool(TechnicalDrawingTool tool)
        {
            RemoveTrendLinePreview();
            _activeDrawingTool = tool;
            _trendLineStart = null;
            _suppressContextMenuAfterCancel = false;
            Chart.ReleaseMouseCapture();
            Chart.UserInputProcessor.IsEnabled = tool == TechnicalDrawingTool.Select && !_textDrawingActive;
            UpdateTechnicalDrawingButtons();
            Chart.Refresh();
        }

        private void UpdateTechnicalDrawingButtons()
        {
            DrawingSelectButton.Opacity = _activeDrawingTool == TechnicalDrawingTool.Select && !_textDrawingActive ? 1.0 : 0.55;
            DrawingTrendLineButton.Opacity = _activeDrawingTool == TechnicalDrawingTool.TrendLine ? 1.0 : 0.55;
            DrawingHorizontalLineButton.Opacity = _activeDrawingTool == TechnicalDrawingTool.HorizontalLine ? 1.0 : 0.55;
            DrawingVerticalLineButton.Opacity = _activeDrawingTool == TechnicalDrawingTool.VerticalLine ? 1.0 : 0.55;
            DrawingRayButton.Opacity = _activeDrawingTool == TechnicalDrawingTool.Ray ? 1.0 : 0.55;
            DrawingTextButton.Opacity = _textDrawingActive ? 1.0 : 0.55;
        }

        private void TechnicalDrawing_MouseDown(object sender, WpfMouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || _textDrawingActive) return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates coordinates)) return;

            if (_activeDrawingTool == TechnicalDrawingTool.HorizontalLine)
            {
                var drawing = new HorizontalLineDrawing { Y = coordinates.Y };
                _horizontalLines.Add(drawing); AddHorizontalLineToChart(drawing);
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | خط افقی رسم شد | قیمت: {coordinates.Y:N2}";
                Chart.Refresh(); e.Handled = true; return;
            }
            if (_activeDrawingTool == TechnicalDrawingTool.VerticalLine)
            {
                int index = FindNearestDrawingBarIndex(coordinates.X); if (index < 0) return;
                var drawing = new VerticalLineDrawing { X = GetDrawingX(index) };
                _verticalLines.Add(drawing); AddVerticalLineToChart(drawing);
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | خط عمودی رسم شد";
                Chart.Refresh(); e.Handled = true; return;
            }
            if (_activeDrawingTool == TechnicalDrawingTool.Ray)
            {
                int index = FindNearestDrawingBarIndex(coordinates.X); if (index < 0) return;
                double x = GetDrawingX(index);
                var ray = new RayDrawing { X1 = x, Y1 = coordinates.Y, X2 = x + 1.0, Y2 = coordinates.Y };
                _rays.Add(ray); AddRayToChart(ray);
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | نیم‌خط افقی رسم شد؛ برای نیم‌خط بعدی کلیک کنید";
                Chart.Refresh(); e.Handled = true; return;
            }
            if (_activeDrawingTool != TechnicalDrawingTool.TrendLine) return;

            // Trend-line points use the actual chart coordinates. This avoids rejecting
            // a valid click when bar-index snapping cannot resolve the current X coordinate.
            var point = coordinates;
            if (_trendLineStart == null)
            {
                _trendLineStart = point;
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | خط روند: نقطه دوم را کلیک کنید";
                e.Handled = true; return;
            }
            if (Math.Abs(point.X - _trendLineStart.Value.X) < 1e-12 && Math.Abs(point.Y - _trendLineStart.Value.Y) < 1e-12) return;
            var trend = new TrendLineDrawing { X1 = _trendLineStart.Value.X, Y1 = _trendLineStart.Value.Y, X2 = point.X, Y2 = point.Y };
            _trendLines.Add(trend); AddTrendLineToChart(trend); RemoveTrendLinePreview(); _trendLineStart = null;
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | خط روند رسم شد؛ برای خط بعدی دوباره کلیک کنید";
            Chart.Refresh(); e.Handled = true;
        }

        private void TechnicalDrawing_MouseMove(object sender, WpfMouseEventArgs e)
        {
            if (_textDrawingActive || _trendLineStart == null || _activeDrawingTool != TechnicalDrawingTool.TrendLine) return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates coordinates)) return;
            RenderTrendLinePreview(coordinates);
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | خط روند: نقطه دوم را انتخاب کنید | قیمت: {coordinates.Y:N2}";
        }

        private void TechnicalDrawing_RightMouseDown(object sender, WpfMouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right) return;
            if (_activeDrawingTool != TechnicalDrawingTool.Select || _textDrawingActive) { CancelDrawingMode(); e.Handled = true; return; }
            if (_suppressContextMenuAfterCancel) { _suppressContextMenuAfterCancel = false; e.Handled = true; return; }
            if (e.ClickCount == 2)
            {
                WpfPoint position = e.GetPosition(Chart); double scale = Chart.DisplayScale; if (scale <= 0) scale = 1.0;
                Chart.ShowContextMenu(new ScottPlot.Pixel(position.X * scale, position.Y * scale)); e.Handled = true;
            }
        }

        private void TechnicalDrawing_KeyDown(object sender, WpfKeyEventArgs e)
        {
            if (e.Key != Key.Escape) return;
            if (_activeDrawingTool == TechnicalDrawingTool.Select && !_textDrawingActive) return;
            CancelDrawingMode(); e.Handled = true;
        }

        private void CancelDrawingMode()
        {
            RemoveTrendLinePreview(); _trendLineStart = null; _textDrawingActive = false; Chart.ReleaseMouseCapture();
            _activeDrawingTool = TechnicalDrawingTool.Select; Chart.UserInputProcessor.IsEnabled = true; _suppressContextMenuAfterCancel = false;
            UpdateTechnicalDrawingButtons(); ChartInfoTextBlock.Text = $"{_symbol.Symbol} | رسم ابزار لغو شد"; Chart.Refresh();
        }

        private void RenderTrendLinePreview(ScottPlot.Coordinates end)
        {
            if (_trendLineStart == null) return;
            RemoveTrendLinePreview();
            _trendLinePreview = Chart.Plot.Add.ScatterLine(new[] { _trendLineStart.Value.X, end.X }, new[] { _trendLineStart.Value.Y, end.Y });
            ApplyScatterStyle(_trendLinePreview, "TrendLine"); Chart.Refresh();
        }

        private void RemoveTrendLinePreview() { if (_trendLinePreview == null) return; Chart.Plot.Remove(_trendLinePreview); _trendLinePreview = null; }
        private void TechnicalDrawing_ChartTypeChanged(object? sender, System.Windows.Controls.SelectionChangedEventArgs e) => QueueTechnicalDrawingRender();

        private void QueueTechnicalDrawingRender()
        {
            if (!IsLoaded || (_trendLines.Count == 0 && _horizontalLines.Count == 0 && _verticalLines.Count == 0 && _rays.Count == 0)) return;
            Dispatcher.BeginInvoke(new Action(() => { if (!IsLoaded) return; RenderTechnicalDrawings(); RenderTextDrawings(); RenderDrawingSelectionOverlay(); Chart.Refresh(); }), DispatcherPriority.ApplicationIdle);
        }

        private int FindNearestDrawingBarIndex(double x)
        {
            if (_bars.Count == 0) return -1; int bestIndex = -1; double bestDistance = double.MaxValue;
            for (int i = 0; i < _bars.Count; i++) { double distance = Math.Abs(GetDrawingX(i) - x); if (distance < bestDistance) { bestDistance = distance; bestIndex = i; } }
            return bestIndex;
        }

        private double GetDrawingX(int index) => _continuousTimeAxisApplied ? ContinuousX(index) : GetBarDateTime(_bars[index], index).ToOADate();

        private void ApplyScatterStyle(ScottPlot.Plottables.Scatter line, string key)
        {
            var style = GetDrawingToolStyle(key); line.MarkerSize = 0; line.LineWidth = (float)Math.Max(0.5, style.LineWidth);
            line.LineColor = ScottPlot.Color.FromHtml(style.Color); line.LinePattern = GetDrawingLinePattern(style.LineStyle);
        }

        private void AddTrendLineToChart(TrendLineDrawing drawing)
        {
            var line = Chart.Plot.Add.ScatterLine(new[] { drawing.X1, drawing.X2 }, new[] { drawing.Y1, drawing.Y2 });
            ApplyScatterStyle(line, "TrendLine"); drawing.PlotLine = line;
        }

        private void AddHorizontalLineToChart(HorizontalLineDrawing drawing)
        {
            var line = Chart.Plot.Add.HorizontalLine(drawing.Y); var style = GetDrawingToolStyle("HorizontalLine");
            line.LineWidth = (float)Math.Max(0.5, style.LineWidth); line.LineColor = ScottPlot.Color.FromHtml(style.Color); line.LinePattern = GetDrawingLinePattern(style.LineStyle); drawing.PlotLine = line;
        }

        private void AddVerticalLineToChart(VerticalLineDrawing drawing)
        {
            var line = Chart.Plot.Add.VerticalLine(drawing.X); var style = GetDrawingToolStyle("VerticalLine");
            line.LineWidth = (float)Math.Max(0.5, style.LineWidth); line.LineColor = ScottPlot.Color.FromHtml(style.Color); line.LinePattern = GetDrawingLinePattern(style.LineStyle); drawing.PlotLine = line;
        }

        private void AddRayToChart(RayDrawing drawing)
        {
            var limits = Chart.Plot.Axes.GetLimits();
            var line = Chart.Plot.Add.ScatterLine(new[] { drawing.X1, limits.Right }, new[] { drawing.Y1, drawing.Y1 });
            ApplyScatterStyle(line, "HorizontalRay"); drawing.PlotLine = line;
        }

        private void RenderTechnicalDrawings()
        {
            foreach (var drawing in _trendLines) AddTrendLineToChart(drawing);
            foreach (var drawing in _horizontalLines) AddHorizontalLineToChart(drawing);
            foreach (var drawing in _verticalLines) AddVerticalLineToChart(drawing);
            foreach (var drawing in _rays) AddRayToChart(drawing);
        }
    }
}
