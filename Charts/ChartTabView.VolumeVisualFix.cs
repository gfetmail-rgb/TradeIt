using System;
using System.Linq;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        /// <summary>
        /// Keep the volume data rectangle horizontally aligned with the price
        /// data rectangle without copying the price rectangle's vertical size.
        /// The two WpfPlot controls have different heights, so copying the full
        /// PixelRect was incorrect and compressed the volume bars vertically.
        /// </summary>
        private void ApplyVolumeVisualFrame()
        {
            VolumeChart.Plot.FigureBackground.Color = ScottPlot.Color.FromHtml("#FFFFFF");
            VolumeChart.Plot.DataBackground.Color = ScottPlot.Color.FromHtml("#FFFFFF");

            VolumeChart.Plot.Axes.Left.IsVisible = false;
            VolumeChart.Plot.Axes.Right.IsVisible = false;
            VolumeChart.Plot.Axes.Top.IsVisible = false;
            VolumeChart.Plot.Axes.Bottom.IsVisible = false;

            // The custom DataBorder is the only border around the data area.
            // It uses the exact same style as the price chart.
            VolumeChart.Plot.Axes.Frame(false);
            VolumeChart.Plot.DataBorder = new ScottPlot.LineStyle
            {
                Color = ScottPlot.Color.FromHtml(_settings.AxisColor),
                Width = 1,
                Pattern = ScottPlot.LinePattern.Solid
            };

            // The outer figure frame is also shared with the price chart.
            VolumeChart.Plot.FigureBorder = new ScottPlot.LineStyle
            {
                Color = ScottPlot.Color.FromHtml(_settings.AxisColor),
                Width = 1,
                Pattern = ScottPlot.LinePattern.Solid
            };

            // PixelPadding uses float values in the ScottPlot version used by
            // this project. The axis widths are kept as doubles elsewhere.
            VolumeChart.Plot.Layout.Fixed(
                new ScottPlot.PixelPadding(
                    left: (float)LeftAxisWidth,
                    right: (float)RightAxisWidth,
                    top: 0f,
                    bottom: 0f));

            // Volume bars are intentionally monochrome for now.
            foreach (var plottable in VolumeChart.Plot.GetPlottables())
            {
                if (plottable is ScottPlot.Plottables.BarPlot barPlot)
                {
                    foreach (var bar in barPlot.Bars)
                    {
                        bar.FillColor = ScottPlot.Color.FromHtml("#000000");
                        bar.LineColor = ScottPlot.Color.FromHtml("#000000");
                    }
                }
            }
        }

        private void AlignVolumeDataRectToPrice()
        {
            // Do not copy the price chart's complete DataRect: that would also
            // copy its height and make the volume bars occupy only a fraction
            // of the available volume panel.
            ApplyVolumeVisualFrame();
        }
    }
}
