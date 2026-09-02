using System;
using System.Linq;

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

                // The volume axis is based on the actual maximum volume.
                // Do not use percentile-based scaling, which can distort the
                // visual relationship between normal and exceptional volumes.
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
                // Axis information may be unavailable during initial rendering.
                // The next synchronization pass will retry.
            }
        }
    }
}
