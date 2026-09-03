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
            if (sender is not ChartTabView chart)
                return;

            // The chart has several other Loaded handlers which may rebuild the
            // plottable or restore settings. Queue two ApplicationIdle passes so
            // this is the final range operation after those handlers complete.
            chart.Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    chart.ApplyInitialCandleRange();
                    chart.Dispatcher.BeginInvoke(
                        new Action(chart.ApplyInitialCandleRange),
                        DispatcherPriority.ApplicationIdle);
                }),
                DispatcherPriority.ApplicationIdle);
        }

        private void ApplyInitialCandleRange()
        {
            if (_bars.Count == 0)
                return;

            int visibleCount = Math.Min(InitialVisibleCandleCount, _bars.Count);
            int firstVisibleIndex = _bars.Count - visibleCount;
            int lastVisibleIndex = _bars.Count - 1;

            double firstX = GetBarDateTime(_bars[firstVisibleIndex], firstVisibleIndex).ToOADate();
            double lastX = GetBarDateTime(_bars[lastVisibleIndex], lastVisibleIndex).ToOADate();

            if (!double.IsFinite(firstX) || !double.IsFinite(lastX) || lastX < firstX)
                return;

            const double CandleHalfWidthDays = 0.5;
            var limits = Chart.Plot.Axes.GetLimits();

            Chart.Plot.Axes.SetLimits(
                firstX - CandleHalfWidthDays,
                lastX + CandleHalfWidthDays,
                limits.Bottom,
                limits.Top);

            // Recalculate price limits only from the candles inside the new X
            // range, then store this exact range for Reset Zoom.
            AutoFitVisiblePriceRange();
            SaveInitialView();
            _initialCandleRangeApplied = true;
            Chart.Refresh();
        }
    }
}
