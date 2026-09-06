using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TradeIt.Models;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private const double ContinuousChartBaseDate = 2000.0;
        private bool _continuousTimeAxisApplied;
        private bool _timeGapsRefreshPending;
        private bool _timeGapsEventsAttached;

        private static readonly bool _timeGapsRegistered = RegisterTimeGapsHandling();
        private static readonly bool _dateRangeFixRegistered = RegisterDateRangeFix();

        private static bool RegisterTimeGapsHandling()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent, new RoutedEventHandler(TimeGaps_Loaded));
            return true;
        }

        private static void TimeGaps_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart) return;
            if (!chart._timeGapsEventsAttached)
            {
                chart._timeGapsEventsAttached = true;
                ChartSettingsManager.SettingsChanged += chart.TimeGaps_SettingsChanged;
                chart.Chart.PreviewMouseMove += chart.TimeGaps_ChartMouseMove;
            }
            chart.QueueTimeGapsApplication();
        }

        private void TimeGaps_SettingsChanged(object? sender, EventArgs e)
        {
            if (!IsLoaded) return;
            QueueTimeGapsApplication();
        }

        private void QueueTimeGapsApplication()
        {
            if (_timeGapsRefreshPending) return;
            _timeGapsRefreshPending = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _timeGapsRefreshPending = false;
                ApplyTimeGapsSetting();
            }), DispatcherPriority.ApplicationIdle);
        }

        private void ApplyTimeGapsSetting()
        {
            if (_bars.Count == 0 || !IsLoaded) return;
            bool showGaps = ChartSettingsManager.Current.ShowTimeGaps;
            if (showGaps)
            {
                if (!_continuousTimeAxisApplied) return;
                _continuousTimeAxisApplied = false;
                DrawChart();
                ConfigureFinalDateAxis();
                Chart.Refresh();
                return;
            }
            if (!_continuousTimeAxisApplied) ApplyContinuousTimeAxis();
        }

        private void ApplyContinuousTimeAxis()
        {
            if (_bars.Count == 0) return;
            ClearMainChart();
            DrawContinuousChartSeries();
            ApplySettings();
            ConfigureContinuousDateAxis();

            // The drawing coordinate helpers must know that the chart is already
            // using the continuous index axis before any drawing is restored.
            _continuousTimeAxisApplied = true;

            // ClearMainChart() also removes persisted drawing plottables. Restore the
            // non-advanced drawings here; advanced drawings and their styles are restored
            // by AdvancedDrawingRenderFix during RenderStarting.
            RenderTechnicalDrawings();
            RenderAllFibonacciDrawings();
            RenderArrowDrawings();
            RenderTextDrawings();
            RenderDrawingSelectionOverlay();
            if (_textSelection != null) RenderTextSelectionVisuals();

            RestoreCrosshairAndDateAxis();
            Chart.Refresh();
        }

        private void DrawContinuousChartSeries()
        {
            switch (_chartType)
            {
                case ChartDisplayType.Candlestick: DrawContinuousCandlestick(); break;
                case ChartDisplayType.Line: DrawContinuousLine(); break;
                case ChartDisplayType.Bar: DrawContinuousBar(); break;
            }
        }

        private static double ContinuousX(int index) => ContinuousChartBaseDate + index;

        private void DrawContinuousCandlestick()
        {
            var candles = new List<ScottPlot.OHLC>(_bars.Count);
            for (int i = 0; i < _bars.Count; i++)
            {
                MarketBar bar = _bars[i];
                DateTime time = DateTime.FromOADate(ContinuousX(i));
                candles.Add(new ScottPlot.OHLC(bar.Open, bar.High, bar.Low, bar.Close, time, TimeSpan.FromDays(1)));
            }
            var plot = Chart.Plot.Add.Candlestick(candles);
            plot.RisingColor = ScottPlot.Color.FromHtml(_settings.RisingColor);
            plot.FallingColor = ScottPlot.Color.FromHtml(_settings.FallingColor);
        }

        private void DrawContinuousLine()
        {
            var xs = new double[_bars.Count];
            var ys = new double[_bars.Count];
            for (int i = 0; i < _bars.Count; i++) { xs[i] = ContinuousX(i); ys[i] = _bars[i].Close; }
            if (_bars.Count == 0) return;
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
                bars.Add(new ScottPlot.OHLC(bar.Open, bar.High, bar.Low, bar.Close, time, TimeSpan.FromDays(1)));
            }
            if (bars.Count == 0) return;
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
                int index = tickCount == 1 ? 0 : (int)Math.Round(n * (_bars.Count - 1.0) / (tickCount - 1.0));
                positions[n] = ContinuousX(index);
                string label = HasSourceDate(index) ? GetSourceDateLabel(index) : $"کندل {index + 1}";
                labels[n] = string.IsNullOrWhiteSpace(label) ? $"کندل {index + 1}" : label;
            }
            axis.TickGenerator = new ScottPlot.TickGenerators.NumericManual(positions, labels);
        }

        private void ApplyContinuousCrosshair(WpfMouseEventArgs e)
        {
            if (!_continuousTimeAxisApplied || _crosshair == null || !_crosshairVisible || !_chartVisible) return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates coordinates)) return;
            int index = (int)Math.Round(coordinates.X - ContinuousChartBaseDate);
            if (index < 0 || index >= _bars.Count) return;
            _crosshair.Position = new ScottPlot.Coordinates(ContinuousX(index), coordinates.Y);
            _crosshair.IsVisible = true;
            _crosshairMouseInside = true;
            UpdateCrosshairAxisLabel(index);
            UpdateOHLCVInfo(index);
            Chart.Refresh();
        }

        private void TimeGaps_ChartMouseMove(object sender, WpfMouseEventArgs e)
        {
            if (_continuousTimeAxisApplied) ApplyContinuousCrosshair(e);
        }

        private static bool RegisterDateRangeFix()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent, new RoutedEventHandler(DateRangeFix_Loaded));
            return true;
        }

        private static void DateRangeFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart) return;
            chart.Dispatcher.BeginInvoke(new Action(chart.ApplyDateAndInitialRangeFix), DispatcherPriority.SystemIdle);
        }

        private void ApplyDateAndInitialRangeFix()
        {
            try
            {
                bool changed = NormalizeTimestampsFromSourceDates();
                if (changed)
                    DrawChart();

                InitializeCrosshairAtInitialPosition();
                ConfigureDisplayDateAxis(Chart);
                Chart.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Chart date normalization fix failed: {ex}");
            }
        }

        private bool NormalizeTimestampsFromSourceDates()
        {
            bool changed = false;
            var calendar = new PersianCalendar();
            foreach (MarketBar bar in _bars)
            {
                if (!IsPersianSourceDate(bar)) continue;
                if (!TryParseJalaliDate(bar.JalaliDate, out int year, out int month, out int day)) continue;
                if (TryNormalizeTimestamp(bar, calendar, year, month, day)) changed = true;
            }
            return changed;
        }

        private static bool IsPersianSourceDate(MarketBar bar)
        {
            return string.Equals(bar.Calendar?.Trim(), "Persian", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(bar.JalaliDate);
        }

        private static bool TryParseJalaliDate(string? value, out int year, out int month, out int day)
        {
            year = month = day = 0;
            string date = NormalizeDigits(value).Trim();
            string[] parts = date.Split('/', '-', '.');
            return parts.Length == 3
                && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out year)
                && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out month)
                && int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out day);
        }

        private static bool TryNormalizeTimestamp(MarketBar bar, PersianCalendar calendar, int year, int month, int day)
        {
            try
            {
                DateTime converted = calendar.ToDateTime(year, month, day, 0, 0, 0, 0);
                string time = NormalizeDigits(bar.Time).Trim();
                if (!string.IsNullOrWhiteSpace(time)
                    && TimeSpan.TryParse(time, CultureInfo.InvariantCulture, out TimeSpan timeOfDay))
                {
                    converted = converted.Date.Add(timeOfDay);
                }

                if (bar.Timestamp.HasValue && bar.Timestamp.Value == converted) return false;
                bar.Timestamp = converted;
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        private static string NormalizeDigits(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value
                .Replace('۰', '0').Replace('۱', '1').Replace('۲', '2').Replace('۳', '3').Replace('۴', '4')
                .Replace('۵', '5').Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9')
                .Replace('٠', '0').Replace('١', '1').Replace('٢', '2').Replace('٣', '3').Replace('٤', '4')
                .Replace('٥', '5').Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9');
        }
    }
}
