using System;
using System.Linq;
using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private void SyncVolumeLimitsToPrice()
        {
            if (!_volumeVisible)
                return;

            try
            {
                var priceLimits = Chart.Plot.Axes.GetLimits();
                if (priceLimits.Width <= 0 || priceLimits.Height <= 0)
                    return;

                var bars = VolumeChart.Plot.GetPlottables()
                    .OfType<ScottPlot.Plottables.BarPlot>()
                    .SelectMany(x => x.Bars)
                    .Where(x => double.IsFinite(x.Value) && x.Value >= 0)
                    .ToList();

                if (bars.Count == 0)
                    return;

                // The volume axis must be based on the actual data, not on a
                // percentile or on the current visible price range. Using a
                // percentile here makes many normal bars appear artificially
                // close to the ceiling when a few large-volume bars exist.
                double maxVolume = bars.Max(x => x.Value);
                if (!double.IsFinite(maxVolume) || maxVolume <= 0)
                    maxVolume = 1;

                double top = maxVolume * 1.10;
                if (!double.IsFinite(top) || top <= 0)
                    top = 1;

                VolumeChart.Plot.Axes.SetLimits(
                    priceLimits.Left,
                    priceLimits.Right,
                    0,
                    top);
            }
            catch
            {
                // Layout/axis information may be unavailable during initial
                // rendering. The next synchronization pass will retry.
            }
        }
    }
}
