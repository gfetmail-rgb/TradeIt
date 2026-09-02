using System;
using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private void ApplyVolumeVisualFrame()
        {
            // Keep the volume panel visually bounded on all four sides.
            // The frame is applied to the plot itself so it follows the exact
            // dimensions of the volume chart area.
            VolumeChart.Plot.Axes.Color(ScottPlot.Color.FromHtml("#000000"));
            VolumeChart.Plot.FigureBackground.Color = ScottPlot.Color.FromHtml("#FFFFFF");
            VolumeChart.Plot.DataBackground.Color = ScottPlot.Color.FromHtml("#FFFFFF");
        }
    }
}
