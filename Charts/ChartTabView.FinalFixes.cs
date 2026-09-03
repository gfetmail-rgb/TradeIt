using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using TradeIt.Models;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _finalChartFixesRegistered = RegisterFinalChartFixes();
        private bool _finalChartFixesInitialized;

        private static bool RegisterFinalChartFixes()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(FinalChartFixes_Loaded));
            return true;
        }

        private static void FinalChartFixes_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart || chart._finalChartFixesInitialized)
                return;

            chart._finalChartFixesInitialized = true;
            chart.Dispatcher.BeginInvoke(
                new Action(chart.ApplyFinalChartFixes),
                DispatcherPriority.ContextIdle);
        }

        private void ApplyFinalChartFixes()
        {
            try
            {
                // TimeGaps=false uses a completely different X coordinate system.
                // Do not let this late final-fix pass overwrite its continuous axis
                // with real OADate coordinates/range.
                bool showTimeGaps = ChartSettingsManager.Current.ShowTimeGaps;
                if (showTimeGaps)
                {
                    _continuousTimeAxisApplied = false;
                    ConfigureFinalDateAxis();
                    ForceInitial365CandleRange();
                }
                else
                {
                    ApplyContinuousTimeAxis();
                }

                _settings = ChartSettingsManager.Current;
                _gridVisible = _settings.GridVisible;
                _crosshairVisible = _settings.CrosshairVisible;
                ApplyGridDisplayState();
                ApplyCrosshairDisplayState();
                UpdateDisplayStateButtons();

                UpdateInitialOHLCVInfo();
                Chart.Refresh();

                Chart.MouseMove -= FinalChartFixes_MouseMove;
                Chart.MouseMove += FinalChartFixes_MouseMove;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Final chart fixes failed: {ex}");
            }
        }

        private bool HasSourceDate(int index)
        {
            if (index < 0 || index >= _bars.Count)
                return false;

            MarketBar bar = _bars[index];
            return !string.IsNullOrWhiteSpace(bar.Calendar) &&
                   !string.IsNullOrWhiteSpace(bar.JalaliDate);
        }

        private void ConfigureFinalDateAxis()
        {
            int count = _bars.Count;
            if (count == 0)
                return;

            var axis = Chart.Plot.Axes.NumericTicksBottom();
            int tickCount = Math.Min(9, count);
            var positions = new List<double>(tickCount);
            var labels = new List<string>(tickCount);

            for (int n = 0; n < tickCount; n++)
            {
                int index = tickCount == 1
                    ? 0
                    : (int)Math.Round(n * (count - 1.0) / (tickCount - 1.0));

                positions.Add(GetBarDateTime(_bars[index], index).ToOADate());

                string label = HasSourceDate(index)
                    ? GetSourceDateLabel(index)
                    : $"کندل {index + 1}";

                labels.Add(string.IsNullOrWhiteSpace(label)
                    ? $"کندل {index + 1}"
                    : label);
            }

            axis.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
                positions.ToArray(), labels.ToArray());
        }

        private void ForceInitial365CandleRange()
        {
            if (_bars.Count == 0)
                return;

            const int visibleCount = 365;
            int firstIndex = Math.Max(0, _bars.Count - visibleCount);
            int lastIndex = _bars.Count - 1;

            double firstX = GetBarDateTime(_bars[firstIndex], firstIndex).ToOADate();
            double lastX = GetBarDateTime(_bars[lastIndex], lastIndex).ToOADate();

            if (!double.IsFinite(firstX) || !double.IsFinite(lastX))
                return;

            const double xPadding = 0.5;
            double minPrice = double.MaxValue;
            double maxPrice = double.MinValue;

            for (int i = firstIndex; i <= lastIndex; i++)
            {
                minPrice = Math.Min(minPrice, _bars[i].Low);
                maxPrice = Math.Max(maxPrice, _bars[i].High);
            }

            if (double.IsFinite(minPrice) && double.IsFinite(maxPrice))
            {
                double range = maxPrice - minPrice;
                double padding = range > 0
                    ? range * 0.05
                    : Math.Max(Math.Abs(maxPrice) * 0.01, 1);

                Chart.Plot.Axes.SetLimits(
                    firstX - xPadding,
                    lastX + xPadding,
                    minPrice - padding,
                    maxPrice + padding);
            }
            else
            {
                var current = Chart.Plot.Axes.GetLimits();
                Chart.Plot.Axes.SetLimits(
                    firstX - xPadding,
                    lastX + xPadding,
                    current.Bottom,
                    current.Top);
            }

            _initialCandleRangeApplied = true;
            SaveInitialView();
        }

        private void FinalChartFixes_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            try
            {
                if (_bars.Count == 0)
                    return;

                if (!_continuousTimeAxisApplied)
                {
                    if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates coordinates))
                        return;

                    int index = FindNearestBarIndex(coordinates.X);
                    if (index >= 0)
                        UpdateOHLCVInfo(index);
                }
            }
            catch
            {
            }
        }

        private void UpdateInitialOHLCVInfo()
        {
            if (_bars.Count > 0)
                UpdateOHLCVInfo(_bars.Count - 1);
        }

        private void UpdateOHLCVInfo(int index)
        {
            if (index < 0 || index >= _bars.Count)
                return;

            MarketBar bar = _bars[index];
            string date = HasSourceDate(index)
                ? GetSourceDateLabel(index)
                : $"کندل {index + 1}";

            if (string.IsNullOrWhiteSpace(date))
                date = $"کندل {index + 1}";

            ChartInfoTextBlock.Text =
                $"{_symbol.Symbol} | O: {bar.Open:N2}  H: {bar.High:N2}  L: {bar.Low:N2}  C: {bar.Close:N2}  V: {bar.Volume:N0} | {date}";
        }
    }
}
