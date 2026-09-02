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

                double maxVolume = bars.Max(x => x.Value);
                if (!double.IsFinite(maxVolume) || maxVolume <= 0)
                    maxVolume = 1;

                // Keep only a very small headroom so the largest real volume
                // is visually close to the top of the volume panel.
                double top = maxVolume * 1.02;
                if (!double.IsFinite(top) || top <= 0)
                    top = 1;

                VolumeChart.Plot.Axes.SetLimits(
                    priceLimits.Left,
                    priceLimits.Right,
                    0,
                    top);

                // The X limits are copied from the price chart on every sync,
                // so panning/zooming the price chart keeps the volume window
                // on exactly the same candles.
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
