using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using TradeIt.Models;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _undatedFinalFixRegistered = RegisterUndatedFinalFix();
        private static readonly DateTime UndatedBaseDate = new DateTime(2000, 1, 1);

        private static bool RegisterUndatedFinalFix()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(UndatedFinalFix_Loaded));
            return true;
        }

        private static void UndatedFinalFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart)
                return;

            chart.Dispatcher.BeginInvoke(
                new Action(chart.ApplyUndatedFinalFix),
                DispatcherPriority.ApplicationIdle);
        }

        private void ApplyUndatedFinalFix()
        {
            try
            {
                if (_bars.Count == 0)
                    return;

                bool hasSourceDate = false;
                foreach (MarketBar bar in _bars)
                {
                    if (!string.IsNullOrWhiteSpace(bar.Calendar) &&
                        !string.IsNullOrWhiteSpace(bar.JalaliDate))
                    {
                        hasSourceDate = true;
                        break;
                    }
                }

                if (hasSourceDate)
                    return;

                for (int i = 0; i < _bars.Count; i++)
                    _bars[i].Timestamp = UndatedBaseDate.AddDays(i);

                // Rebuild once so Line/Bar/Candlestick all use the same
                // monotonic internal X coordinates.
                _hasInitialView = false;
                DrawChart();

                var axis = Chart.Plot.Axes.NumericTicksBottom();
                int count = _bars.Count;
                int tickCount = Math.Min(9, count);
                var positions = new List<double>(tickCount);
                var labels = new List<string>(tickCount);

                for (int n = 0; n < tickCount; n++)
                {
                    int index = tickCount == 1
                        ? 0
                        : (int)Math.Round(n * (count - 1.0) / (tickCount - 1.0));

                    positions.Add(_bars[index].Timestamp!.Value.ToOADate());
                    labels.Add($"کندل {index + 1}");
                }

                axis.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
                    positions.ToArray(),
                    labels.ToArray());

                int firstIndex = Math.Max(0, count - 365);
                int lastIndex = count - 1;
                double firstX = _bars[firstIndex].Timestamp!.Value.ToOADate();
                double lastX = _bars[lastIndex].Timestamp!.Value.ToOADate();

                double minPrice = double.MaxValue;
                double maxPrice = double.MinValue;
                for (int i = firstIndex; i <= lastIndex; i++)
                {
                    minPrice = Math.Min(minPrice, _bars[i].Low);
                    maxPrice = Math.Max(maxPrice, _bars[i].High);
                }

                double pricePadding = maxPrice > minPrice
                    ? (maxPrice - minPrice) * 0.05
                    : Math.Max(Math.Abs(maxPrice) * 0.01, 1);

                Chart.Plot.Axes.SetLimits(
                    firstX - 0.5,
                    lastX + 0.5,
                    minPrice - pricePadding,
                    maxPrice + pricePadding);

                _initialCandleRangeApplied = true;
                SaveInitialView();
                UpdateInitialOHLCVInfo();
                Chart.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Undated chart final fix failed: {ex}");
            }
        }
    }
}
