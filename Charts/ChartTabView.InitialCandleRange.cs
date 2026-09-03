using System;
using System.Windows;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private const int InitialVisibleCandleCount = 365;
        private bool _initialCandleRangeApplied;
        private static readonly bool _initialCandleRangeRegistered = RegisterInitialCandleRange();

        private static bool RegisterInitialCandleRange()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(InitialCandleRange_Loaded));
            return true;
        }

        private static void InitialCandleRange_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart || chart._initialCandleRangeApplied)
                return;

            chart.Dispatcher.BeginInvoke(
                new Action(chart.ApplyInitialCandleRange),
                DispatcherPriority.ContextIdle);
        }

        private void ApplyInitialCandleRange()
        {
            if (_initialCandleRangeApplied || _bars.Count == 0)
                return;

            _initialCandleRangeApplied = true;

            // If there are 365 or fewer records, keep the complete dataset visible.
            if (_bars.Count <= InitialVisibleCandleCount)
                return;

            int firstVisibleIndex = _bars.Count - InitialVisibleCandleCount;
            double firstX = GetBarDateTime(_bars[firstVisibleIndex], firstVisibleIndex).ToOADate();
            double lastX = GetBarDateTime(_bars[^1], _bars.Count - 1).ToOADate();

            if (!double.IsFinite(firstX) || !double.IsFinite(lastX) || lastX <= firstX)
                return;

            // Add half a candle of horizontal padding so the first and last
            // candles are not clipped at the edges of the plot area.
            const double CandleHalfWidthDays = 0.5;
            var limits = Chart.Plot.Axes.GetLimits();
            Chart.Plot.Axes.SetLimits(
                firstX - CandleHalfWidthDays,
                lastX + CandleHalfWidthDays,
                limits.Bottom,
                limits.Top);

            // Recalculate the price range from only the 365 candles now visible.
            AutoFitVisiblePriceRange();
            SaveInitialView();
            Chart.Refresh();
        }
    }
}
