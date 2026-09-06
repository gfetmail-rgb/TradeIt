using System;
using System.Windows;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        // Single source of truth for the requested initial chart view.
        // At most 200 candles are visible, with the candles occupying about 2/3
        // of the horizontal plot area and the final 1/3 left empty.
        private const int InitialVisibleCandleCount = 200;
        private const double InitialRightMarginFraction = 1.0 / 3.0;
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
            if (_bars.Count == 0 || _initialCandleRangeApplied)
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
            // To leave one third of the viewport empty, the right margin must
            // be half of the candle span: data width : margin = 2 : 1.
            double rightMargin = candleRange * InitialRightMarginFraction / (1.0 - InitialRightMarginFraction);
            double leftMargin = Math.Max(candleRange * InitialLeftMarginFraction, 0.5);

            var limits = Chart.Plot.Axes.GetLimits();
            Chart.Plot.Axes.SetLimits(
                firstX - leftMargin,
                lastX + rightMargin,
                limits.Bottom,
                limits.Top);

            AutoFitInitialVisiblePriceRange(firstVisibleIndex, lastVisibleIndex);
            SaveInitialView();
            _initialCandleRangeApplied = true;
            Chart.Refresh();
        }

        private void AutoFitInitialVisiblePriceRange(int firstIndex, int lastIndex)
        {
            double minPrice = double.MaxValue;
            double maxPrice = double.MinValue;

            for (int i = firstIndex; i <= lastIndex; i++)
            {
                minPrice = Math.Min(minPrice, _bars[i].Low);
                maxPrice = Math.Max(maxPrice, _bars[i].High);
            }

            if (!double.IsFinite(minPrice) || !double.IsFinite(maxPrice) || minPrice == double.MaxValue || maxPrice == double.MinValue)
                return;

            double range = maxPrice - minPrice;
            double padding = range > 0
                ? range * 0.05
                : Math.Max(Math.Abs(maxPrice) * 0.01, 1.0);

            var limits = Chart.Plot.Axes.GetLimits();
            Chart.Plot.Axes.SetLimits(
                limits.Left,
                limits.Right,
                minPrice - padding,
                maxPrice + padding);
        }

        private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_hasInitialView)
            {
                _initialCandleRangeApplied = false;
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