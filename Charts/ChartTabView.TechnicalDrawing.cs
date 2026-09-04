using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private enum TechnicalDrawingTool { Select, TrendLine, HorizontalLine, VerticalLine, Ray }

        private sealed class TrendLineDrawing { public double X1 { get; init; } public double Y1 { get; init; } public double X2 { get; init; } public double Y2 { get; init; } public ScottPlot.Plottables.Scatter? PlotLine { get; set; } }
        private sealed class HorizontalLineDrawing { public double Y { get; init; } public ScottPlot.Plottables.HorizontalLine? PlotLine { get; set; } }
        private sealed class VerticalLineDrawing { public double X { get; init; } public ScottPlot.Plottables.VerticalLine? PlotLine { get; set; } }
        private sealed class RayDrawing { public double X1 { get; init; } public double Y1 { get; init; } public double X2 { get; init; } public double Y2 { get; init; } public ScottPlot.Plottables.Scatter? PlotLine { get; set; } }

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

            // Register at the UserControl root with handledEventsToo so ScottPlot cannot
            // swallow cancellation before this handler sees it.
            AddHandler(Mouse.PreviewMouseRightButtonDownEvent,
                new MouseButtonEventHandler(TechnicalDrawing_RightMouseDown), true);
            AddHandler(Keyboard.PreviewKeyDownEvent,
                new KeyEventHandler(TechnicalDrawing_KeyDown), true);

            Focusable = true;
            ChartTypeComboBox.SelectionChanged += TechnicalDrawing_ChartTypeChanged;
            ChartSettingsManager.SettingsChanged += TechnicalDrawing_SettingsChanged;
            UpdateTechnicalDrawingButtons();
        }

        private void DrawingSelectButton_Click(object sender, RoutedEventArgs e) => SetTechnicalDrawingTool(TechnicalDrawingTool.Select);

        private void DrawingTrendLineButton_Click(object sender, RoutedEventArgs e)
        {
            _textDrawingActive = false;
            SetTechnicalDrawingTool(TechnicalDrawingTool.TrendLine);
            FocusChartForDrawing();
        }

        private void DrawingHorizontalLineButton_Click(object sender, RoutedEventArgs e)
        {
            _textDrawingActive = false;
            SetTechnicalDrawingTool(TechnicalDrawingTool.HorizontalLine);
            FocusChartForDrawing();
        }

        private void DrawingVerticalLineButton_Click(object sender, RoutedEventArgs e)
        {
            _textDrawingActive = false;
            SetTechnicalDrawingTool(TechnicalDrawingTool.VerticalLine);
            FocusChartForDrawing();
        }

        private void DrawingRayButton_Click(object sender, RoutedEventArgs e)
        {
            _textDrawingActive = false;
            SetTechnicalDrawingTool(TechnicalDrawingTool.Ray);
            FocusChartForDrawing();
        }

        private void FocusChartForDrawing()
        {
            Chart.Focusable = true;
            Chart.Focus();
            Focus();
        }

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

            if (_activeDrawingTool == TechnicalDrawingTool.HorizontalLine)
            {
                if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates coordinates)) return;
                var drawing = new HorizontalLineDrawing { Y = coordinates.Y };
                _horizontalLines.Add(drawing);
                AddHorizontalLineToChart(drawing);
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | خط افقی رسم شد | قیمت: {coordinates.Y:N2}";
                Chart.Refresh();
                e.Handled = true;
                return;
            }

            if (_activeDrawingTool == TechnicalDrawingTool.VerticalLine)
            {
                if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates coordinates)) return;
                int index = FindNearestDrawingBarIndex(coordinates.X);
                if (index < 0) return;
                var drawing = new VerticalLineDrawing { X = GetDrawingX(index) };
                _verticalLines.Add(drawing);
                AddVerticalLineToChart(drawing);
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | خط عمودی رسم شد";
                Chart.Refresh();
                e.Handled = true;
                return;
            }

            if (_activeDrawingTool != TechnicalDrawingTool.TrendLine && _activeDrawingTool != TechnicalDrawingTool.Ray) return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates drawingCoordinates)) return;
            int drawingIndex = FindNearestDrawingBarIndex(drawingCoordinates.X);
            if (drawingIndex < 0) return;
            var point = new ScottPlot.Coordinates(GetDrawingX(drawingIndex), drawingCoordinates.Y);

            if (_trendLineStart == null)
            {
                _trendLineStart = point;
                ChartInfoTextBlock.Text = _activeDrawingTool == TechnicalDrawingTool.Ray
                    ? $"{_symbol.Symbol} | نیم‌خط: نقطه اول انتخاب شد؛ برای تعیین جهت کلیک کنید"
                    : $"{_symbol.Symbol} | خط روند: نقطه اول انتخاب شد؛ برای نقطه دوم کلیک کنید";
                e.Handled = true;
                return;
            }

            if (Math.Abs(point.X - _trendLineStart.Value.X) < 1e-12 && Math.Abs(point.Y - _trendLineStart.Value.Y) < 1e-12) return;

            if (_activeDrawingTool == TechnicalDrawingTool.Ray)
            {
                var ray = new RayDrawing { X1 = _trendLineStart.Value.X, Y1 = _trendLineStart.Value.Y, X2 = point.X, Y2 = point.Y };
                _rays.Add(ray);
                AddRayToChart(ray);
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | نیم‌خط رسم شد";
            }
            else
            {
                var drawing = new TrendLineDrawing { X1 = _trendLineStart.Value.X, Y1 = _trendLineStart.Value.Y, X2 = point.X, Y2 = point.Y };
                _trendLines.Add(drawing);
                AddTrendLineToChart(drawing);
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | خط روند رسم شد؛ برای خط بعدی دوباره کلیک کنید.";
            }

            RemoveTrendLinePreview();
            _trendLineStart = null;
            Chart.Refresh();
            e.Handled = true;
        }

        private void TechnicalDrawing_MouseMove(object sender, WpfMouseEventArgs e)
        {
            if (_textDrawingActive || _trendLineStart == null) return;
            if (_activeDrawingTool != TechnicalDrawingTool.TrendLine && _activeDrawingTool != TechnicalDrawingTool.Ray) return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates coordinates)) return;
            int index = FindNearestDrawingBarIndex(coordinates.X);
            if (index < 0) return;
            var end = new ScottPlot.Coordinates(GetDrawingX(index), coordinates.Y);
            RenderTrendLinePreview(end, _activeDrawingTool == TechnicalDrawingTool.Ray);
            ChartInfoTextBlock.Text = _activeDrawingTool == TechnicalDrawingTool.Ray
                ? $"{_symbol.Symbol} | نیم‌خط: جهت را انتخاب کنید | قیمت: {coordinates.Y:N2}"
                : $"{_symbol.Symbol} | خط روند: نقطه دوم را انتخاب کنید | قیمت: {coordinates.Y:N2}";
        }

        private void TechnicalDrawing_RightMouseDown(object sender, WpfMouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right) return;
            if (_activeDrawingTool != TechnicalDrawingTool.Select || _textDrawingActive)
            {
                CancelDrawingMode();
                e.Handled = true;
                return;
            }
            if (_suppressContextMenuAfterCancel)
            {
                _suppressContextMenuAfterCancel = false;
                e.Handled = true;
                return;
            }
            if (e.ClickCount == 2)
            {
                WpfPoint position = e.GetPosition(Chart);
                double scale = Chart.DisplayScale;
                if (scale <= 0) scale = 1.0;
                Chart.ShowContextMenu(new ScottPlot.Pixel(position.X * scale, position.Y * scale));
                e.Handled = true;
            }
        }

        private void TechnicalDrawing_KeyDown(object sender, WpfKeyEventArgs e)
        {
            if (e.Key != Key.Escape) return;
            if (_activeDrawingTool == TechnicalDrawingTool.Select && !_textDrawingActive) return;
            CancelDrawingMode();
            e.Handled = true;
        }

        private void CancelDrawingMode()
        {
            RemoveTrendLinePreview();
            _trendLineStart = null;
            _textDrawingActive = false;
            Chart.ReleaseMouseCapture();
            _activeDrawingTool = TechnicalDrawingTool.Select;
            Chart.UserInputProcessor.IsEnabled = true;
            _suppressContextMenuAfterCancel = false;
            UpdateTechnicalDrawingButtons();
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | رسم ابزار لغو شد";
            Chart.Refresh();
        }

        private void RenderTrendLinePreview(ScottPlot.Coordinates end, bool ray)
        {
            if (_trendLineStart == null) return;
            RemoveTrendLinePreview();
            double endX = end.X;
            double endY = end.Y;
            if (ray)
            {
                var limits = Chart.Plot.Axes.GetLimits();
                double dx = end.X - _trendLineStart.Value.X;
                if (Math.Abs(dx) > 1e-12)
                {
                    endX = dx > 0 ? limits.Right : limits.Left;
                    endY = _trendLineStart.Value.Y + (end.Y - _trendLineStart.Value.Y) / dx * (endX - _trendLineStart.Value.X);
                }
            }
            _trendLinePreview = Chart.Plot.Add.ScatterLine(new[] { _trendLineStart.Value.X, endX }, new[] { _trendLineStart.Value.Y, endY });
            _trendLinePreview.MarkerSize = 0;
            _trendLinePreview.LineWidth = (float)Math.Max(1.0, _settings.LineWidth);
            _trendLinePreview.LineColor = ScottPlot.Color.FromHtml(_settings.LineColor);
            Chart.Refresh();
        }

        private void RemoveTrendLinePreview()
        {
            if (_trendLinePreview == null) return;
            Chart.Plot.Remove(_trendLinePreview);
            _trendLinePreview = null;
        }

        private void TechnicalDrawing_ChartTypeChanged(object? sender, System.Windows.Controls.SelectionChangedEventArgs e) => QueueTechnicalDrawingRender();
        private void TechnicalDrawing_SettingsChanged(object? sender, EventArgs e) => QueueTechnicalDrawingRender();

        private void QueueTechnicalDrawingRender()
        {
            if (!IsLoaded || (_trendLines.Count == 0 && _horizontalLines.Count == 0 && _verticalLines.Count == 0 && _rays.Count == 0)) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!IsLoaded) return;
                RenderTechnicalDrawings();
                RenderTextDrawings();
                Chart.Refresh();
            }), DispatcherPriority.ApplicationIdle);
        }

        private int FindNearestDrawingBarIndex(double x)
        {
            if (_bars.Count == 0) return -1;
            int bestIndex = -1;
            double bestDistance = double.MaxValue;
            for (int i = 0; i < _bars.Count; i++)
            {
                double distance = Math.Abs(GetDrawingX(i) - x);
                if (distance < bestDistance) { bestDistance = distance; bestIndex = i; }
            }
            return bestIndex;
        }

        private double GetDrawingX(int index) => _continuousTimeAxisApplied ? ContinuousX(index) : GetBarDateTime(_bars[index], index).ToOADate();

        private void AddTrendLineToChart(TrendLineDrawing drawing)
        {
            var line = Chart.Plot.Add.ScatterLine(new[] { drawing.X1, drawing.X2 }, new[] { drawing.Y1, drawing.Y2 });
            line.MarkerSize = 0;
            line.LineWidth = (float)Math.Max(1.0, _settings.LineWidth);
            line.LineColor = ScottPlot.Color.FromHtml(_settings.LineColor);
            drawing.PlotLine = line;
        }

        private void AddHorizontalLineToChart(HorizontalLineDrawing drawing)
        {
            var line = Chart.Plot.Add.HorizontalLine(drawing.Y);
            line.LineWidth = (float)Math.Max(1.0, _settings.LineWidth);
            line.LineColor = ScottPlot.Color.FromHtml(_settings.LineColor);
            drawing.PlotLine = line;
        }

        private void AddVerticalLineToChart(VerticalLineDrawing drawing)
        {
            var line = Chart.Plot.Add.VerticalLine(drawing.X);
            line.LineWidth = (float)Math.Max(1.0, _settings.LineWidth);
            line.LineColor = ScottPlot.Color.FromHtml(_settings.LineColor);
            drawing.PlotLine = line;
        }

        private void AddRayToChart(RayDrawing drawing)
        {
            var limits = Chart.Plot.Axes.GetLimits();
            double dx = drawing.X2 - drawing.X1;
            double endX;
            double endY;
            if (Math.Abs(dx) < 1e-12)
            {
                endX = drawing.X1;
                endY = drawing.Y2 >= drawing.Y1 ? limits.Top : limits.Bottom;
            }
            else
            {
                endX = dx > 0 ? limits.Right : limits.Left;
                endY = drawing.Y1 + (drawing.Y2 - drawing.Y1) / dx * (endX - drawing.X1);
            }
            var line = Chart.Plot.Add.ScatterLine(new[] { drawing.X1, endX }, new[] { drawing.Y1, endY });
            line.MarkerSize = 0;
            line.LineWidth = (float)Math.Max(1.0, _settings.LineWidth);
            line.LineColor = ScottPlot.Color.FromHtml(_settings.LineColor);
            drawing.PlotLine = line;
        }

        private void RenderTechnicalDrawings()
        {
            RemoveTrendLinePreview();
            foreach (TrendLineDrawing drawing in _trendLines)
            {
                if (drawing.PlotLine != null) Chart.Plot.Remove(drawing.PlotLine);
                AddTrendLineToChart(drawing);
            }
            foreach (HorizontalLineDrawing drawing in _horizontalLines)
            {
                if (drawing.PlotLine != null) Chart.Plot.Remove(drawing.PlotLine);
                AddHorizontalLineToChart(drawing);
            }
            foreach (VerticalLineDrawing drawing in _verticalLines)
            {
                if (drawing.PlotLine != null) Chart.Plot.Remove(drawing.PlotLine);
                AddVerticalLineToChart(drawing);
            }
            foreach (RayDrawing drawing in _rays)
            {
                if (drawing.PlotLine != null) Chart.Plot.Remove(drawing.PlotLine);
                AddRayToChart(drawing);
            }
        }
    }
}
