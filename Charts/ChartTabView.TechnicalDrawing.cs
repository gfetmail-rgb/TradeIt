using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private enum TechnicalDrawingTool
        {
            Select,
            TrendLine
        }

        private sealed class TrendLineDrawing
        {
            public double X1 { get; init; }
            public double Y1 { get; init; }
            public double X2 { get; init; }
            public double Y2 { get; init; }
            public ScottPlot.Plottables.Scatter? PlotLine { get; set; }
        }

        private readonly List<TrendLineDrawing> _trendLines = new();
        private TechnicalDrawingTool _activeDrawingTool = TechnicalDrawingTool.Select;
        private ScottPlot.Coordinates? _trendLineStart;
        private bool _technicalDrawingEventsAttached;

        private static readonly bool _technicalDrawingRegistered = RegisterTechnicalDrawingHandling();

        private static bool RegisterTechnicalDrawingHandling()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(TechnicalDrawing_Loaded));
            return true;
        }

        private static void TechnicalDrawing_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart || chart._technicalDrawingEventsAttached)
                return;

            chart._technicalDrawingEventsAttached = true;
            chart.Chart.PreviewMouseLeftButtonDown += chart.TechnicalDrawing_MouseDown;
            chart.Chart.PreviewMouseMove += chart.TechnicalDrawing_MouseMove;
            chart.Chart.PreviewMouseLeftButtonUp += chart.TechnicalDrawing_MouseUp;
            chart.KeyDown += chart.TechnicalDrawing_KeyDown;
            chart.ChartTypeComboBox.SelectionChanged += chart.TechnicalDrawing_ChartTypeChanged;
            ChartSettingsManager.SettingsChanged += chart.TechnicalDrawing_SettingsChanged;
            chart.UpdateTechnicalDrawingButtons();
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

        private void SetTechnicalDrawingTool(TechnicalDrawingTool tool)
        {
            _activeDrawingTool = tool;
            _trendLineStart = null;
            Chart.ReleaseMouseCapture();
            Chart.UserInputProcessor.IsEnabled = tool == TechnicalDrawingTool.Select;
            UpdateTechnicalDrawingButtons();
            Chart.Refresh();
        }

        private void UpdateTechnicalDrawingButtons()
        {
            DrawingSelectButton.Opacity = _activeDrawingTool == TechnicalDrawingTool.Select ? 1.0 : 0.55;
            DrawingTrendLineButton.Opacity = _activeDrawingTool == TechnicalDrawingTool.TrendLine ? 1.0 : 0.55;
        }

        private void TechnicalDrawing_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_activeDrawingTool != TechnicalDrawingTool.TrendLine || e.ChangedButton != MouseButton.Left)
                return;

            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates coordinates))
                return;

            int index = FindNearestDrawingBarIndex(coordinates.X);
            if (index < 0)
                return;

            double x = GetDrawingX(index);
            double y = coordinates.Y;
            var point = new ScottPlot.Coordinates(x, y);

            if (_trendLineStart == null)
            {
                _trendLineStart = point;
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | خط روند: نقطه اول انتخاب شد";
                e.Handled = true;
                return;
            }

            if (Math.Abs(point.X - _trendLineStart.Value.X) < 1e-12 &&
                Math.Abs(point.Y - _trendLineStart.Value.Y) < 1e-12)
                return;

            var drawing = new TrendLineDrawing
            {
                X1 = _trendLineStart.Value.X,
                Y1 = _trendLineStart.Value.Y,
                X2 = point.X,
                Y2 = point.Y
            };

            _trendLines.Add(drawing);
            AddTrendLineToChart(drawing);
            _trendLineStart = null;
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | خط روند رسم شد";
            Chart.Refresh();
            e.Handled = true;
        }

        private void TechnicalDrawing_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_activeDrawingTool != TechnicalDrawingTool.TrendLine || _trendLineStart == null)
                return;

            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates coordinates))
                return;

            int index = FindNearestDrawingBarIndex(coordinates.X);
            if (index < 0)
                return;

            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | خط روند: نقطه دوم را انتخاب کنید | قیمت: {coordinates.Y:N2}";
        }

        private void TechnicalDrawing_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_activeDrawingTool == TechnicalDrawingTool.TrendLine &&
                e.ChangedButton == MouseButton.Left &&
                _trendLineStart != null)
            {
                e.Handled = true;
            }
        }

        private void TechnicalDrawing_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _activeDrawingTool == TechnicalDrawingTool.TrendLine)
            {
                _trendLineStart = null;
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | خط روند: لغو شد";
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
            if (!IsLoaded || _trendLines.Count == 0)
                return;

            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    if (!IsLoaded || _trendLines.Count == 0)
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

        private void RenderTechnicalDrawings()
        {
            foreach (TrendLineDrawing drawing in _trendLines)
            {
                if (drawing.PlotLine != null)
                    Chart.Plot.Remove(drawing.PlotLine);

                AddTrendLineToChart(drawing);
            }
        }
    }
}
