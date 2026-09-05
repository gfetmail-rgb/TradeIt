using System;
using System.Windows;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _initialRightMarginFixRegistered = RegisterInitialRightMarginFix();
        private bool _initialRightMarginFixQueued;

        private static bool RegisterInitialRightMarginFix()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(InitialRightMarginFix_Loaded));
            return true;
        }

        private static void InitialRightMarginFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart || chart._initialRightMarginFixQueued)
                return;

            chart._initialRightMarginFixQueued = true;
            chart.Dispatcher.BeginInvoke(
                new Action(chart.ApplyInitialRightMarginFix),
                DispatcherPriority.Render);
        }

        private void ApplyInitialRightMarginFix()
        {
            if (_bars.Count == 0 || !IsLoaded || _continuousTimeAxisApplied || !_hasInitialView)
                return;

            int visibleCount = Math.Min(365, _bars.Count);
            int firstIndex = _bars.Count - visibleCount;
            int lastIndex = _bars.Count - 1;

            double firstX = GetBarDateTime(_bars[firstIndex], firstIndex).ToOADate();
            double lastX = GetBarDateTime(_bars[lastIndex], lastIndex).ToOADate();
            if (!double.IsFinite(firstX) || !double.IsFinite(lastX) || lastX < firstX)
                return;

            double candleRange = Math.Max(1.0, lastX - firstX);
            double rightMargin = candleRange * InitialRightMarginFraction / (1.0 - InitialRightMarginFraction);
            var limits = Chart.Plot.Axes.GetLimits();

            Chart.Plot.Axes.SetLimits(
                firstX - 0.5,
                lastX + 0.5 + rightMargin,
                limits.Bottom,
                limits.Top);

            SaveInitialView();
            Chart.Refresh();
        }
    }
}
