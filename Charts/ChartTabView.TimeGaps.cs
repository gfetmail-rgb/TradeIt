using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using TradeIt.Models;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private const double ContinuousChartBaseDate = 2000.0;
        private bool _continuousTimeAxisApplied;
        private bool _timeGapsRefreshPending;

        private static readonly bool _timeGapsRegistered = RegisterTimeGapsHandling();

        private static bool RegisterTimeGapsHandling()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(TimeGaps_Loaded));
            return true;
        }

        private static void TimeGaps_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart)
                return;

            chart.QueueTimeGapsApplication();
        }

        private void QueueTimeGapsApplication()
        {
            if (_timeGapsRefreshPending)
                return;

            _timeGapsRefreshPending = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _timeGapsRefreshPending = false;
                ApplyTimeGapsSetting();
            }), DispatcherPriority.ApplicationIdle);
        }

        private void ApplyTimeGapsSetting()
        {
            if (_bars.Count == 0 || !IsLoaded)
                return;

            bool showGaps = ChartSettingsManager.Current.ShowTimeGaps;
            if (showGaps)
            {
                if (!_continuousTimeAxisApplied)
                    return;

                _continuousTimeAxisApplied = false;
                DrawChart();
                ConfigureFinalDateAxis();
                Chart.Refresh();
                return;
            }

            ApplyContinuousTimeAxis();
        }

        private void ApplyContinuousTimeAxis()
        {
            if (_bars.Count == 0)
                return;

            ClearMainChart();

            switch (_chartType)
            {
                case ChartDisplayType.Candlestick:
                    DrawContinuousCandlestick();
                    break;
                case ChartDisplayType.Line:
                    DrawContinuousLine();
                    break;
                case ChartDisplayType.Bar:
                    DrawContinuousBar();
                    break;
            }

            ApplySettings();
            ConfigureContinuousDateAxis();

            int visibleCount = Math.Min(365, _bars.Count);
            int firstIndex = _bars.Count - visibleCount;
            int lastIndex = _bars.Count - 1;

            var current = Chart.Plot.Axes.GetLimits();
            double firstX = ContinuousX(firstIndex);
            double lastX = ContinuousX(lastIndex);
            double minPrice = double.MaxValue;
            double maxPrice = double.MinValue;

            for (int i = firstIndex; i <= lastIndex; i++)
            {
                minPrice = Math.Min(minPrice, _bars[i].Low);
                maxPrice = Math.Max(maxPrice, _bars[i].High);
            }

            double padding = maxPrice > minPrice
                ? (maxPrice - minPrice) * 0.05
                : Math.Max(Math.Abs(maxPrice) * 0.01, 1);

            Chart.Plot.Axes.SetLimits(
                firstX - 0.5,
                lastX + 0.5,
                double.IsFinite(minPrice) ? minPrice - padding : current.Bottom,
                double.IsFinite(maxPrice) ? maxPrice + padding : current.Top);

            SaveInitialView();
            _initialCandleRangeApplied = true;
            _continuousTimeAxisApplied = true;
            Chart.Refresh();
        }

        private static double ContinuousX(int index) => ContinuousChartBaseDate + index;

        private void DrawContinuousCandlestick()
        {
            var candles = new List<ScottPlot.OHLC>(_bars.Count);
            for (int i = 0; i < _bars.Count; i++)
            {
                MarketBar bar = _bars[i];
                DateTime time = DateTime.FromOADate(ContinuousX(i));
                candles.Add(new ScottPlot.OHLC(
                    bar.Open, bar.High, bar.Low, bar.Close,
                    time, TimeSpan.FromDays(1)));
            }

            var plot = Chart.Plot.Add.Candlestick(candles);
            plot.RisingColor = ScottPlot.Color.FromHtml(_settings.RisingColor);
            plot.FallingColor = ScottPlot.Color.FromHtml(_settings.FallingColor);
        }

        private void DrawContinuousLine()
        {
            var xs = new double[_bars.Count];
            var ys = new double[_bars.Count];
            for (int i = 0; i < _bars.Count; i++)
            {
                xs[i] = ContinuousX(i);
                ys[i] = _bars[i].Close;
            }

            if (_bars.Count == 0)
                return;

            var line = Chart.Plot.Add.ScatterLine(xs, ys);
            line.MarkerSize = 0;
            line.LineWidth = (float)Math.Max(0.01, _settings.LineWidth);
            line.LineColor = ScottPlot.Color.FromHtml(_settings.LineColor);
            line.ConnectStyle = ScottPlot.ConnectStyle.Straight;
            line.Smooth = false;
            line.PathStrategy = new ScottPlot.PathStrategies.Straight();
        }

        private void DrawContinuousBar()
        {
            var bars = new List<ScottPlot.OHLC>(_bars.Count);
            for (int i = 0; i < _bars.Count; i++)
            {
                MarketBar bar = _bars[i];
                DateTime time = DateTime.FromOADate(ContinuousX(i));
                bars.Add(new ScottPlot.OHLC(
                    bar.Open, bar.High, bar.Low, bar.Close,
                    time, TimeSpan.FromDays(1)));
            }

            if (bars.Count == 0)
                return;

            var plot = Chart.Plot.Add.OHLC(bars);
            plot.RisingStyle.Color = ScottPlot.Color.FromHtml(_settings.RisingColor);
            plot.FallingStyle.Color = ScottPlot.Color.FromHtml(_settings.FallingColor);
        }

        private void ConfigureContinuousDateAxis()
        {
            var axis = Chart.Plot.Axes.NumericTicksBottom();
            int tickCount = Math.Min(9, _bars.Count);
            var positions = new double[tickCount];
            var labels = new string[tickCount];

            for (int n = 0; n < tickCount; n++)
            {
                int index = tickCount == 1
                    ? 0
                    : (int)Math.Round(n * (_bars.Count - 1.0) / (tickCount - 1.0));
                positions[n] = ContinuousX(index);
                string label = HasSourceDate(index)
                    ? GetSourceDateLabel(index)
                    : $"کندل {index + 1}";
                labels[n] = string.IsNullOrWhiteSpace(label)
                    ? $"کندل {index + 1}"
                    : label;
            }

            axis.TickGenerator = new ScottPlot.TickGenerators.NumericManual(positions, labels);
        }

        private void ApplyContinuousCrosshair(WpfMouseEventArgs e)
        {
            if (!_continuousTimeAxisApplied || _crosshair == null || !_crosshairVisible || !_chartVisible)
                return;

            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates coordinates))
                return;

            int index = (int)Math.Round(coordinates.X - ContinuousChartBaseDate);
            if (index < 0 || index >= _bars.Count)
                return;

            _crosshair.Position = new ScottPlot.Coordinates(ContinuousX(index), coordinates.Y);
            _crosshair.IsVisible = true;
            _crosshairMouseInside = true;
            UpdateCrosshairAxisLabel(index);
            UpdateOHLCVInfo(index);
            Chart.Refresh();
        }

        private void TimeGaps_ChartMouseMove(object sender, WpfMouseEventArgs e)
        {
            ApplyContinuousCrosshair(e);
        }
    }
}
