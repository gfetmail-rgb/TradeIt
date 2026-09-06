using System;
using System.Windows;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        // Single source of truth for the requested initial chart view.
        private const int InitialVisibleCandleCount = 200;
        private const double InitialRightMarginFraction = 0.10;
        private const double InitialLeftMarginFraction = 0.02;
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

            chart.Dispatcher.BeginInvoke(
                new Action(chart.ApplyInitialCandleRange),
                DispatcherPriority.ApplicationIdle);
        }

        private void ApplyInitialCandleRange()
        {
            if (_bars.Count == 0 || _hasInitialView)
                return;

            if (!ChartSettingsManager.Current.ShowTimeGaps)
                return;

            int visibleCount = Math.Min(InitialVisibleCandleCount, _bars.Count);
            int firstVisibleIndex = _bars.Count - visibleCount;
            int lastVisibleIndex = _bars.Count - 1;

            double firstX = GetBarDateTime(_bars[firstVisibleIndex], firstVisibleIndex).ToOADate();
            double lastX = GetBarDateTime(_bars[lastVisibleIndex], lastVisibleIndex).ToOADate();
            if (!double.IsFinite(firstX) || !double.IsFinite(lastX) || lastX < firstX)
                return;

            double candleRange = Math.Max(1.0, lastX - firstX);
            double rightMargin = candleRange * InitialRightMarginFraction;
            double leftMargin = Math.Max(candleRange * InitialLeftMarginFraction, 0.5);

            var limits = Chart.Plot.Axes.GetLimits();
            Chart.Plot.Axes.SetLimits(
                firstX - leftMargin,
                lastX + rightMargin,
                limits.Bottom,
                limits.Top);

            SaveInitialView();
            _initialCandleRangeApplied = true;
            Chart.Refresh();
        }

        private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_hasInitialView)
            {
                ApplyInitialCandleRange();
                return;
            }

            Chart.Plot.Axes.SetLimits(
                _initialXMin,
                _initialXMax,
                _initialYMin,
                _initialYMax);
            Chart.Refresh();
        }
    }
}
