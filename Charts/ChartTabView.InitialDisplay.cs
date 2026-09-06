using System;
using System.Windows;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private const int InitialVisibleCandleCount = 200;
        private const double InitialRightBlankFraction = 0.25;
        private bool _initialDisplayApplied;
        private static readonly bool _initialDisplayRegistered = RegisterInitialDisplay();

        private static bool RegisterInitialDisplay()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(InitialDisplay_Loaded));
            return true;
        }

        private static void InitialDisplay_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart)
                return;

            chart.Dispatcher.BeginInvoke(
                new Action(chart.ApplyInitialDisplayRange),
                DispatcherPriority.Render);
        }

        private void ApplyInitialDisplayRange()
        {
            if (_initialDisplayApplied || _bars.Count == 0 || !IsLoaded)
                return;

            if (Chart.ActualWidth <= 0 || Chart.ActualHeight <= 0)
            {
                Dispatcher.BeginInvoke(
                    new Action(ApplyInitialDisplayRange),
                    DispatcherPriority.Render);
                return;
            }

            try
            {
                Chart.Plot.Axes.AutoScale();

                int visibleCount = Math.Min(InitialVisibleCandleCount, _bars.Count);
                int firstIndex = _bars.Count - visibleCount;
                int lastIndex = _bars.Count - 1;

                double firstX;
                double lastX;

                if (_continuousTimeAxisApplied)
                {
                    firstX = ContinuousX(firstIndex);
                    lastX = ContinuousX(lastIndex);
                }
                else
                {
                    firstX = GetBarDateTime(_bars[firstIndex], firstIndex).ToOADate();
                    lastX = GetBarDateTime(_bars[lastIndex], lastIndex).ToOADate();
                }

                double candleWidth = Math.Max(1.0 / 24.0, lastX > firstX
                    ? (lastX - firstX) / Math.Max(1, visibleCount - 1)
                    : 1.0);

                double left = firstX - candleWidth / 2.0;
                double dataWidth = Math.Max(candleWidth, (lastX - firstX) + candleWidth);
                double rightBlankWidth = dataWidth *
                    InitialRightBlankFraction / (1.0 - InitialRightBlankFraction);
                double right = lastX + candleWidth / 2.0 + rightBlankWidth;

                Chart.Plot.Axes.SetLimitsX(left, right);
                _initialDisplayApplied = true;
                Chart.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Initial chart display range failed: {ex}");
            }
        }
    }
}
