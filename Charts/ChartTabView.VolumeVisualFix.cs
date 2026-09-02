using System;
using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private void ApplyVolumeVisualFrame()
        {
            // Draw the border around the actual ScottPlot data rectangle,
            // not around the larger WPF container. The data rectangle is now
            // horizontally aligned with the price chart by DisplayFixes.
            VolumeChart.Plot.FigureBackground.Color = ScottPlot.Color.FromHtml("#FFFFFF");
            VolumeChart.Plot.DataBackground.Color = ScottPlot.Color.FromHtml("#FFFFFF");
            VolumeChart.Plot.Axes.Color(ScottPlot.Color.FromHtml("#000000"));

            VolumeChart.Plot.Axes.Frame(false);
            VolumeChart.Plot.DataBorder = new ScottPlot.LineStyle
            {
                Color = ScottPlot.Color.FromHtml("#000000"),
                Width = 1,
                Pattern = ScottPlot.LinePattern.Solid
            };
        }
    }
}
