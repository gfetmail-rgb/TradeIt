using System;
using System.Windows;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _initialViewLayoutFinalizationPending = true;
        private bool _initialViewLayoutFinalizationQueued;
        private static readonly bool _initialViewLayoutFixRegistered = RegisterInitialViewLayoutFix();

        private static bool RegisterInitialViewLayoutFix()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(InitialViewLayoutFix_Loaded));
            return true;
        }

        private static void InitialViewLayoutFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart)
                return;

            chart.Chart.SizeChanged -= chart.InitialViewLayoutFix_SizeChanged;
            chart.Chart.SizeChanged += chart.InitialViewLayoutFix_SizeChanged;
            chart.QueueInitialViewLayoutFinalization();
        }

        private void InitialViewLayoutFix_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            QueueInitialViewLayoutFinalization();
        }

        private void QueueInitialViewLayoutFinalization()
        {
            if (!_initialViewLayoutFinalizationPending ||
                _initialViewLayoutFinalizationQueued ||
                !IsLoaded ||
                Chart.ActualWidth <= 0 ||
                Chart.ActualHeight <= 0)
                return;

            _initialViewLayoutFinalizationQueued = true;
            Dispatcher.BeginInvoke(
                new Action(FinalizeInitialViewAfterLayout),
                DispatcherPriority.Render);
        }

        private void FinalizeInitialViewAfterLayout()
        {
            _initialViewLayoutFinalizationQueued = false;

            if (!_initialViewLayoutFinalizationPending ||
                !IsLoaded ||
                Chart.ActualWidth <= 0 ||
                Chart.ActualHeight <= 0 ||
                _bars.Count == 0)
                return;

            try
            {
                // This is deliberately performed after the first real Chart layout.
                // ScottPlot receives its final data-area dimensions only after WPF
                // Measure/Arrange, so the opening view must be finalized here rather
                // than only during the earlier Loaded/idle sequence.
                _initialCandleRangeApplied = false;

                if (ChartSettingsManager.Current.ShowTimeGaps)
                {
                    ApplyInitialCandleRange();
                }
                else
                {
                    ApplyContinuousInitialLimits();
                    SaveInitialView();
                    ApplySavedInitialView();
                    _initialCandleRangeApplied = true;
                }

                if (!_hasInitialView)
                    return;

                ApplySavedInitialView();
                _initialViewLayoutFinalizationPending = false;
                Chart.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Initial chart layout fix failed: {ex}");
            }
        }
    }
}
