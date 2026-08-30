using System.Windows;
using WpfPoint = System.Windows.Point;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        // =========================================================
        // Crosshair Mouse Move
        // =========================================================
        //
        // X is snapped to the nearest candle.
        // Y follows the mouse continuously.
        //
        // The two axis labels move with the crosshair:
        //   - Horizontal axis: date or candle number
        //   - Vertical axis: price
        // =========================================================

        protected override void OnMouseMove(WpfMouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_crosshair == null ||
                !_chartVisible ||
                !_crosshairVisible ||
                !_crosshairMouseInside)
            {
                return;
            }

            ScottPlot.WPF.WpfPlot? sourceChart = null;

            if (Chart.IsMouseOver)
            {
                sourceChart = Chart;
            }
            else if (_volumeVisible &&
                     VolumeChart.IsMouseOver)
            {
                sourceChart = VolumeChart;
            }

            if (sourceChart == null)
                return;

            WpfPoint mousePosition =
                e.GetPosition(sourceChart);

            if (!TryGetChartCoordinates(
                    sourceChart,
                    mousePosition,
                    out ScottPlot.Coordinates mouseCoordinates))
            {
                return;
            }

            // -----------------------------------------------------
            // Snap X to the nearest candle.
            // Y remains continuous and follows the mouse.
            // -----------------------------------------------------

            double snappedX =
                GetNearestCandleX(
                    mouseCoordinates.X);

            double snappedY =
                mouseCoordinates.Y;

            _crosshair.Position =
                new ScottPlot.Coordinates(
                    snappedX,
                    snappedY);

            _crosshair.IsVisible = true;

            // -----------------------------------------------------
            // Axis labels
            // -----------------------------------------------------

            _crosshair.VerticalLine.Label.IsVisible = true;
            _crosshair.HorizontalLine.Label.IsVisible = true;

            _crosshair.VerticalLine.Label.Text =
                FormatCrosshairX(snappedX);

            _crosshair.HorizontalLine.Label.Text =
                snappedY.ToString("N2");

            // -----------------------------------------------------
            // Label appearance
            // -----------------------------------------------------

            _crosshair.VerticalLine.Label.BackgroundColor =
                ScottPlot.Color.FromHtml("#202020");

            _crosshair.VerticalLine.Label.ForeColor =
                ScottPlot.Color.FromHtml("#FFFFFF");

            _crosshair.HorizontalLine.Label.BackgroundColor =
                ScottPlot.Color.FromHtml("#202020");

            _crosshair.HorizontalLine.Label.ForeColor =
                ScottPlot.Color.FromHtml("#FFFFFF");

            _crosshair.VerticalLine.ExcludeFromLegend = true;
            _crosshair.HorizontalLine.ExcludeFromLegend = true;

            // -----------------------------------------------------
            // Existing information box
            // -----------------------------------------------------

            UpdateMouseInformation(
                new ScottPlot.Coordinates(
                    snappedX,
                    snappedY));

            Chart.Refresh();
        }
    }
}