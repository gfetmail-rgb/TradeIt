using System;
using System.Windows.Input;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        // This partial class adds the crosshair behavior without changing
        // the existing chart drawing code.
        // X is snapped to the nearest candle while Y follows the mouse smoothly.
        protected override void OnMouseMove(MouseEventArgs e)
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
            else if (_volumeVisible && VolumeChart.IsMouseOver)
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

            double snappedX =
                GetNearestCandleX(mouseCoordinates.X);

            double snappedY =
                mouseCoordinates.Y;

            // Keep the crosshair on a real candle position horizontally.
            // The vertical position remains continuous and follows the mouse.
            _crosshair.Position =
                new ScottPlot.Coordinates(
                    snappedX,
                    snappedY);

            _crosshair.IsVisible = true;

            // The Crosshair exposes its VerticalLine and HorizontalLine.
            // Their labels are rendered directly on the corresponding axes.
            _crosshair.VerticalLine.Label.IsVisible = true;
            _crosshair.HorizontalLine.Label.IsVisible = true;

            _crosshair.VerticalLine.Label.Text =
                FormatCrosshairX(snappedX);

            _crosshair.HorizontalLine.Label.Text =
                snappedY.ToString("N2");

            // Keep the labels readable and visually consistent with the
            // existing crosshair styling.
            _crosshair.VerticalLine.Label.BackColor =
                ScottPlot.Color.FromHtml("#202020");

            _crosshair.VerticalLine.Label.ForeColor =
                ScottPlot.Color.FromHtml("#FFFFFF");

            _crosshair.HorizontalLine.Label.BackColor =
                ScottPlot.Color.FromHtml("#202020");

            _crosshair.HorizontalLine.Label.ForeColor =
                ScottPlot.Color.FromHtml("#FFFFFF");

            _crosshair.VerticalLine.ExcludeFromLegend = true;
            _crosshair.HorizontalLine.ExcludeFromLegend = true;

            // Preserve the existing information box, but make its X value
            // agree with the snapped crosshair position.
            UpdateMouseInformation(
                new ScottPlot.Coordinates(
                    snappedX,
                    snappedY));

            Chart.Refresh();
        }

        private double GetNearestCandleX(double mouseX)
        {
            if (_bars.Count == 0)
                return mouseX;

            double bestX =
                GetBarDateTime(_bars[0], 0).ToOADate();

            double bestDistance =
                Math.Abs(bestX - mouseX);

            for (int i = 1; i < _bars.Count; i++)
            {
                double x =
                    GetBarDateTime(_bars[i], i).ToOADate();

                double distance =
                    Math.Abs(x - mouseX);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestX = x;
                }
            }

            return bestX;
        }

        private string FormatCrosshairX(double x)
        {
            if (!HasRealDates)
            {
                int candleNumber =
                    GetNearestCandleIndex(x);

                return $"کندل {candleNumber}";
            }

            try
            {
                DateTime dateTime =
                    DateTime.FromOADate(x);

                return dateTime.TimeOfDay == TimeSpan.Zero
                    ? dateTime.ToString("yyyy/MM/dd")
                    : dateTime.ToString("yyyy/MM/dd HH:mm");
            }
            catch
            {
                return x.ToString("N2");
            }
        }
    }
}
