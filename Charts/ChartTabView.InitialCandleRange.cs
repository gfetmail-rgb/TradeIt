using System;
using System.Windows;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private const int InitialVisibleCandleCount = 50;
        private const double InitialRightMarginFraction = 0.25;
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

            chart.Dispatcher.BeginInvoke(new Action(chart.ApplyInitialCandleRange), DispatcherPriority.ApplicationIdle);
        }

        private void ApplyInitialCandleRange()
        {
            if (_bars.Count == 0)
                return;

            // When time gaps are hidden, the chart uses a continuous X coordinate
            // system (2000 + candle index). Do not overwrite those limits with
            // OADate values, otherwise _initialXMin/_initialXMax become inconsistent
            // with the active chart coordinate system and all zoom operations break.
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
            double rightMargin = candleRange * InitialRightMarginFraction / (1.0 - InitialRightMarginFraction);
            const double CandleHalfWidthDays = 0.5;
            var limits = Chart.Plot.Axes.GetLimits();

            Chart.Plot.Axes.SetLimits(
                firstX - CandleHalfWidthDays,
                lastX + CandleHalfWidthDays + rightMargin,
                limits.Bottom,
                limits.Top);

            SaveInitialView();
            _initialCandleRangeApplied = true;
            Chart.Refresh();
        }
    }
}
