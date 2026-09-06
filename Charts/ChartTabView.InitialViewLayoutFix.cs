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

            // Force one render after every Loaded handler has had its chance to
            // configure the chart. The RenderStarting callback below is the final
            // authority for the opening limits.
            chart.Dispatcher.BeginInvoke(
                new Action(chart.Chart.Refresh),
                DispatcherPriority.ContextIdle);
        }

        private void AttachInitialViewRenderFix()
        {
            if (_initialViewRenderFixAttached)
                return;

            _initialViewRenderFixAttached = true;
            Chart.Plot.RenderManager.RenderStarting += InitialViewRenderStarting;
        }

        private void InitialViewRenderStarting(object? sender, ScottPlot.RenderPack e)
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

                // Deliberately use the exact same operation as Reset Zoom.
                // This removes any distinction between the opening view and Reset Zoom.
                ApplySavedInitialView();
                _initialViewRenderFixApplied = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Initial chart render fix failed: {ex}");
            }
        }
    }
}
