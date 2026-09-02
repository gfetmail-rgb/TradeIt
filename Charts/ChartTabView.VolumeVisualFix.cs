using System;
using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        /// <summary>
        /// Make the Volume plot's data rectangle use exactly the same pixel
        /// rectangle as the Price plot. This prevents the Volume area from
        /// extending farther to the right (or starting at a different X).
        /// The simple DataBorder then draws a four-sided box around that area.
        /// </summary>
        private void ApplyVolumeVisualFrame()
        {
            VolumeChart.Plot.FigureBackground.Color = ScottPlot.Color.FromHtml("#FFFFFF");
            VolumeChart.Plot.DataBackground.Color = ScottPlot.Color.FromHtml("#FFFFFF");

            // We do not want real axis lines/ticks to form the frame.
            // The border below is the only frame around the volume data area.
            VolumeChart.Plot.Axes.Left.IsVisible = false;
            VolumeChart.Plot.Axes.Right.IsVisible = false;
            VolumeChart.Plot.Axes.Top.IsVisible = false;
            VolumeChart.Plot.Axes.Bottom.IsVisible = false;

            VolumeChart.Plot.Axes.Frame(false);
            VolumeChart.Plot.DataBorder = new ScottPlot.LineStyle
            {
                Color = ScottPlot.Color.FromHtml("#000000"),
                Width = 1,
                Pattern = ScottPlot.LinePattern.Solid
            };
        }

        /// <summary>
        /// Copy the already-rendered Price chart data rectangle to the Volume
        /// chart. ScottPlot calculates data-area width from its axis panels,
        /// so simply giving both plots identical coordinate limits is not
        /// sufficient to guarantee identical pixel boundaries.
        /// </summary>
        private void AlignVolumeDataRectToPrice()
        {
            try
            {
                var priceLayout = Chart.Plot.LastRender.Layout;
                var priceDataRect = priceLayout.DataRect;

                if (priceDataRect.Width <= 0 || priceDataRect.Height <= 0)
                    return;

                VolumeChart.Plot.Layout.Fixed(priceDataRect);
                ApplyVolumeVisualFrame();
            }
            catch
            {
                // Layout information is not available until the price plot
                // has rendered at least once. The next sync tick will retry.
            }
        }
    }
}
