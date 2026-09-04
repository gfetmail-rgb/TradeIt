using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WpfMouseButtonEventHandler = System.Windows.Input.MouseButtonEventHandler;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private enum TechnicalDrawingTool
        {
            Select,
            TrendLine,
            HorizontalLine
        }

        private sealed class TrendLineDrawing
        {
            public double X1 { get; init; }
            public double Y1 { get; init; }
            public double X2 { get; init; }
            public double Y2 { get; init; }
            public ScottPlot.Plottables.Scatter? PlotLine { get; set; }
        }

        private sealed class HorizontalLineDrawing
        {
            public double Y { get; init; }
            public ScottPlot.Plottables.HorizontalLine? PlotLine { get; set; }
        }

        private readonly List<TrendLineDrawing> _trendLines = new();
        private readonly List<HorizontalLineDrawing> _horizontalLines = new();
        private TechnicalDrawingTool _activeDrawingTool = TechnicalDrawingTool.Select;
        private ScottPlot.Coordinates? _trendLineStart;
        private ScottPlot.Plottables.Scatter? _trendLinePreview;
        private bool _technicalDrawingEventsAttached;
        private bool _suppressContextMenuAfterCancel;

        private void InitializeTechnicalDrawingHandling()
        {
            if (_technicalDrawingEventsAttached)
                return;

            _technicalDrawingEventsAttached = true;
            Chart.PreviewMouseLeftButtonDown += TechnicalDrawing_MouseDown;
            Chart.PreviewMouseMove += TechnicalDrawing_MouseMove;
            Chart.AddHandler(
                UIElement.PreviewMouseRightButtonDownEvent,
                new WpfMouseButtonEventHandler(TechnicalDrawing_RightMouseDown),
                true);
            KeyDown += TechnicalDrawing_KeyDown;
            ChartTypeComboBox.SelectionChanged += TechnicalDrawing_ChartTypeChanged;
            ChartSettingsManager.SettingsChanged += TechnicalDrawing_SettingsChanged;
            UpdateTechnicalDrawingButtons();
        }

        private void DrawingSelectButton_Click(object sender, RoutedEventArgs e)
        {
            SetTechnicalDrawingTool(TechnicalDrawingTool.Select);
        }

        private void DrawingTrendLineButton_Click(object sender, RoutedEventArgs e)
        {
            SetTechnicalDrawingTool(TechnicalDrawingTool.TrendLine);
            Chart.Focus();
        }

        private void DrawingHorizontalLineButton_Click(object sender, RoutedEventArgs e)
        {
            SetTechnicalDrawingTool(TechnicalDrawingTool.HorizontalLine);
            Chart.Focus();
        }

        private void SetTechnicalDrawingTool(TechnicalDrawingTool tool)
        {
            RemoveTrendLinePreview();
            _activeDrawingTool = tool;
            _trendLineStart = null;
            _suppressContextMenuAfterCancel = false;
            Chart.ReleaseMouseCapture();
            Chart.UserInputProcessor.IsEnabled = tool == TechnicalDrawingTool.Select;
            UpdateTechnicalDrawingButtons();
            Chart.Refresh();
        }

        private void UpdateTechnicalDrawingButtons()
        {
            DrawingSelectButton.Opacity = _activeDrawingTool == TechnicalDrawingTool.Select ? 1.0 : 0.55;
            DrawingTrendLineButton.Opacity = _activeDrawingTool == TechnicalDrawingTool.TrendLine ? 1.0 : 0.55;
            DrawingHorizontalLineButton.Opacity = _activeDrawingTool == TechnicalDrawingTool.HorizontalLine ? 1.0 : 0.55;
        }

        private void TechnicalDrawing_MouseDown(object sender, WpfMouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (_activeDrawingTool == TechnicalDrawingTool.HorizontalLine)
            {
                if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates coordinates))
                    return;

                var drawing = new HorizontalLineDrawing { Y = coordinates.Y };
                _horizontalLines.Add(drawing);
                AddHorizontalLineToChart(drawing);
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | خط افقی رسم شد | قیمت: {coordinates.Y:N2}";
                Chart.Refresh();
                e.Handled = true;
                return;
            }

            if (_activeDrawingTool != TechnicalDrawingTool.TrendLine)
                return;

            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates trendCoordinates))
                return;

            int index = FindNearestDrawingBarIndex(trendCoordinates.X);
            if (index < 0)
                return;

            double x = GetDrawingX(index);
            double y = trendCoordinates.Y;
            var point = new ScottPlot.Coordinates(x, y);

            if (_trendLineStart == null)
            {
                _trendLineStart = point;
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | خط روند: نقطه اول انتخاب شد؛ برای نقطه دوم کلیک کنید";
                e.Handled = true;
                return;
            }

            if (Math.Abs(point.X - _trendLineStart.Value.X) < 1e-12 &&
                Math.Abs(point.Y - _trendLineStart.Value.Y) < 1e-12)
                return;

            var trendDrawing = new TrendLineDrawing
            {
                X1 = _trendLineStart.Value.X,
                Y1 = _trendLineStart.Value.Y,
                X2 = point.X,
                Y2 = point.Y
            };

            _trendLines.Add(trendDrawing);
            AddTrendLineToChart(trendDrawing);
            RemoveTrendLinePreview();
            _trendLineStart = null;
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | خط روند رسم شد؛ برای خط بعدی دوباره کلیک کنید";
            Chart.Refresh();
            e.Handled = true;
        }

        private void TechnicalDrawing_MouseMove(object sender, WpfMouseEventArgs e)
        {
            if (_activeDrawingTool != TechnicalDrawingTool.TrendLine || _trendLineStart == null)
                return;

            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates coordinates))
                return;

            int index = FindNearestDrawingBarIndex(coordinates.X);
            if (index < 0)
                return;

            var end = new ScottPlot.Coordinates(GetDrawingX(index), coordinates.Y);
            RenderTrendLinePreview(end);
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | خط روند: نقطه دوم را انتخاب کنید | قیمت: {coordinates.Y:N2}";
        }

        private void TechnicalDrawing_RightMouseDown(object sender, WpfMouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right)
                return;

            if (_activeDrawingTool != TechnicalDrawingTool.Select)
            {
                SetTechnicalDrawingTool(TechnicalDrawingTool.Select);
                _suppressContextMenuAfterCancel = true;
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | رسم ابزار لغو شد";
                Chart.Refresh();
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
            }

            e.Handled = true;
        }

        private void RenderTrendLinePreview(ScottPlot.Coordinates end)
        {
            if (_trendLineStart == null)
                return;

            RemoveTrendLinePreview();
            _trendLinePreview = Chart.Plot.Add.ScatterLine(
                new[] { _trendLineStart.Value.X, end.X },
                new[] { _trendLineStart.Value.Y, end.Y });
            _trendLinePreview.MarkerSize = 0;
            _trendLinePreview.LineWidth = (float)Math.Max(1.0, _settings.LineWidth);
            _trendLinePreview.LineColor = ScottPlot.Color.FromHtml(_settings.LineColor);
            Chart.Refresh();
        }

        private void RemoveTrendLinePreview()
        {
            if (_trendLinePreview == null)
                return;

            Chart.Plot.Remove(_trendLinePreview);
            _trendLinePreview = null;
        }

        private void TechnicalDrawing_KeyDown(object sender, WpfKeyEventArgs e)
        {
            if (e.Key == Key.Escape && _activeDrawingTool != TechnicalDrawingTool.Select)
            {
                RemoveTrendLinePreview();
                _trendLineStart = null;
                Chart.ReleaseMouseCapture();
                SetTechnicalDrawingTool(TechnicalDrawingTool.Select);
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | رسم ابزار لغو شد";
                Chart.Refresh();
                e.Handled = true;
            }
        }

        private void TechnicalDrawing_ChartTypeChanged(object? sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            QueueTechnicalDrawingRender();
        }

        private void TechnicalDrawing_SettingsChanged(object? sender, EventArgs e)
        {
            QueueTechnicalDrawingRender();
        }

        private void QueueTechnicalDrawingRender()
        {
            if (!IsLoaded || (_trendLines.Count == 0 && _horizontalLines.Count == 0))
                return;

            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    if (!IsLoaded || (_trendLines.Count == 0 && _horizontalLines.Count == 0))
                        return;
                    RenderTechnicalDrawings();
                    Chart.Refresh();
                }),
                DispatcherPriority.ApplicationIdle);
        }

        private int FindNearestDrawingBarIndex(double x)
        {
            if (_bars.Count == 0)
                return -1;

            int bestIndex = -1;
            double bestDistance = double.MaxValue;

            for (int i = 0; i < _bars.Count; i++)
            {
                double barX = GetDrawingX(i);
                double distance = Math.Abs(barX - x);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private double GetDrawingX(int index)
        {
            return _continuousTimeAxisApplied
                ? ContinuousX(index)
                : GetBarDateTime(_bars[index], index).ToOADate();
        }

        private void AddTrendLineToChart(TrendLineDrawing drawing)
        {
            var line = Chart.Plot.Add.ScatterLine(
                new[] { drawing.X1, drawing.X2 },
                new[] { drawing.Y1, drawing.Y2 });

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

        private void RenderTechnicalDrawings()
        {
            RemoveTrendLinePreview();

            foreach (TrendLineDrawing drawing in _trendLines)
            {
                if (drawing.PlotLine != null)
                    Chart.Plot.Remove(drawing.PlotLine);

                AddTrendLineToChart(drawing);
            }

            foreach (HorizontalLineDrawing drawing in _horizontalLines)
            {
                if (drawing.PlotLine != null)
                    Chart.Plot.Remove(drawing.PlotLine);

                AddHorizontalLineToChart(drawing);
            }
        }
    }
}
