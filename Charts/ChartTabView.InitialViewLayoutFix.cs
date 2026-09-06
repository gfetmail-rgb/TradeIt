using System;
using System.Windows;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _initialViewRenderFixAttached;
        private bool _initialViewRenderFixApplied;
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

            chart.AttachInitialViewRenderFix();

            // Start one render after all Loaded handlers have had an opportunity to
            // configure the chart. The final initial-view correction is performed
            // from RenderFinished, after ScottPlot has completed the whole pipeline.
            chart.Dispatcher.BeginInvoke(
                new Action(chart.Chart.Refresh),
                DispatcherPriority.ContextIdle);
        }

        private void AttachInitialViewRenderFix()
        {
            if (_initialViewRenderFixAttached)
                return;

            _initialViewRenderFixAttached = true;
            Chart.Plot.RenderManager.RenderFinished += InitialViewRenderFinished;
        }

        private void InitialViewRenderFinished(object? sender, ScottPlot.RenderDetails e)
        {
            if (_initialViewRenderFixApplied || !IsLoaded || _bars.Count == 0)
                return;

            try
            {
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

                // Reset Zoom uses this exact operation. Performing it after the
                // complete ScottPlot render pipeline prevents any earlier autoscale,
                // plottable axis manager, or layout pass from overwriting the opening
                // limits.
                ApplySavedInitialView();
                _initialViewRenderFixApplied = true;

                // SetLimits() changes the axis state, but the frame just completed.
                // Render once more so the first visible frame is guaranteed to use
                // the same limits stored for Reset Zoom.
                Chart.Dispatcher.BeginInvoke(
                    new Action(Chart.Refresh),
                    DispatcherPriority.Render);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Initial chart render-finished fix failed: {ex}");
            }
        }
    }
}
