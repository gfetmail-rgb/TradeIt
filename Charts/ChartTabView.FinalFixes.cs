using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
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
                DispatcherPriority.ApplicationIdle);
        }

        private void ApplyFinalChartFixes()
        {
            try
            {
                ConfigureFinalDateAxis();
                ForceInitial365CandleRange();
                _crosshairVisible = true;
                InitializeCrosshairAtInitialPosition();
                if (_crosshair != null)
                    _crosshair.IsVisible = true;
                CrosshairButton.Content = "Crosshair روشن";
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

        private void ConfigureFinalDateAxis()
        {
            int count = _bars.Count;
            if (count == 0)
                return;

            // The axis is always numeric. Its labels are taken directly from
            // the source file, so Persian dates remain Persian, Gregorian dates
            // remain Gregorian, and files without dates show candle numbers.
            var axis = Chart.Plot.Axes.NumericTicksBottom();
            int tickCount = Math.Min(9, count);
            var positions = new List<double>(tickCount);
            var labels = new List<string>(tickCount);

            for (int n = 0; n < tickCount; n++)
            {
                int index = tickCount == 1
                    ? 0
                    : (int)Math.Round(n * (count - 1.0) / (tickCount - 1.0));

                double x = GetBarDateTime(_bars[index], index).ToOADate();
                positions.Add(x);

                string label = GetSourceDateLabel(index);
                if (string.IsNullOrWhiteSpace(label))
                    label = $"کندل {index + 1}";
                labels.Add(label);
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

            if (!double.IsFinite(firstX) || !double.IsFinite(lastX) || lastX <= firstX)
                return;

            const double halfCandle = 0.5;
            var current = Chart.Plot.Axes.GetLimits();

            Chart.Plot.Axes.SetLimits(
                firstX - halfCandle,
                lastX + halfCandle,
                current.Bottom,
                current.Top);

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
                    firstX - halfCandle,
                    lastX + halfCandle,
                    minPrice - padding,
                    maxPrice + padding);
            }

            _initialCandleRangeApplied = true;
            SaveInitialView();
        }

        private void FinalChartFixes_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_bars.Count == 0 || !TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates coordinates))
                    return;

                int index = FindNearestBarIndex(coordinates.X);
                if (index >= 0)
                    UpdateOHLCVInfo(index);
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
            string date = GetSourceDateLabel(index);
            if (string.IsNullOrWhiteSpace(date))
                date = $"کندل {index + 1}";

            ChartInfoTextBlock.Text =
                $"{_symbol.Symbol} | O: {bar.Open:N2}  H: {bar.High:N2}  L: {bar.Low:N2}  C: {bar.Close:N2}  V: {bar.Volume:N0} | {date}";
        }
    }
}
