using System;
using System.Linq;
using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _volumeAxisSyncHooked;

        static ChartTabView()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnChartTabViewLoadedForVolumeSync));
        }

        private static void OnChartTabViewLoadedForVolumeSync(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not ChartTabView view || view._volumeAxisSyncHooked)
                return;

            view._volumeAxisSyncHooked = true;

            // ScottPlot raises this event whenever the price axis limits change,
            // including left-click drag panning. Keep the volume X window synced.
            view.Chart.Plot.RenderManager.AxisLimitsChanged +=
                view.Chart_Plot_AxisLimitsChangedForVolume;
        }

        private void Chart_Plot_AxisLimitsChangedForVolume(
            object? sender,
            ScottPlot.RenderDetails e)
        {
            if (!_volumeVisible)
                return;

            SyncVolumeLimitsToPrice();
            VolumeChart.Refresh();
        }

        private void SyncVolumeLimitsToPrice()
        {
            if (!_volumeVisible)
                return;

            try
            {
                var priceLimits = Chart.Plot.Axes.GetLimits();
                if (priceLimits.Right <= priceLimits.Left ||
                    priceLimits.Top <= priceLimits.Bottom)
                    return;

                var bars = VolumeChart.Plot.GetPlottables()
                    .OfType<ScottPlot.Plottables.BarPlot>()
                    .SelectMany(x => x.Bars)
                    .Where(x => double.IsFinite(x.Value) && x.Value >= 0)
                    .ToList();

                if (bars.Count == 0)
                    return;

                double maxVolume = bars.Max(x => x.Value);
                if (!double.IsFinite(maxVolume) || maxVolume <= 0)
                    maxVolume = 1;

                // Only 2% headroom: the largest volume bar should be close to
                // the top without being clipped.
                double top = maxVolume * 1.02;
                if (!double.IsFinite(top) || top <= 0)
                    top = 1;

                VolumeChart.Plot.Axes.SetLimits(
                    priceLimits.Left,
                    priceLimits.Right,
                    0,
                    top);

                AlignVolumeDataRectToPrice();
            }
            catch
            {
                // Axis information may be unavailable during initial rendering.
                // The next synchronization pass will retry.
            }
        }
    }
}
