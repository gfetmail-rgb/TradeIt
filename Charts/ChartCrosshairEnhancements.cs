using System;
using System.Windows;
using WpfPoint = System.Windows.Point;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        // X snaps to the nearest candle and Y follows the mouse continuously.
        // The crosshair labels are updated together with the crosshair.
        protected override void OnMouseMove(WpfMouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_crosshair == null ||
                !_chartVisible ||
                !_crosshairVisible ||
                !_crosshairMouseInside)
                return;

            ScottPlot.WPF.WpfPlot? sourceChart = null;

            if (Chart.IsMouseOver)
                sourceChart = Chart;
            else if (_volumeVisible && VolumeChart.IsMouseOver)
                sourceChart = VolumeChart;

            if (sourceChart == null)
                return;

            WpfPoint mousePosition = e.GetPosition(sourceChart);

            if (!TryGetChartCoordinates(
                    sourceChart,
                    mousePosition,
                    out ScottPlot.Coordinates mouseCoordinates))
                return;

            double snappedX = GetNearestCandleX(mouseCoordinates.X);
            double snappedY = mouseCoordinates.Y;

            _crosshair.Position =
                new ScottPlot.Coordinates(snappedX, snappedY);

            _crosshair.IsVisible = true;

            // ScottPlot crosshair labels are attached to the lines.
            // Keep them visible and update their values continuously.
            _crosshair.VerticalLine.Label.IsVisible = true;
            _crosshair.HorizontalLine.Label.IsVisible = true;

            _crosshair.VerticalLine.Label.Text =
                FormatCrosshairX(snappedX);
            _crosshair.HorizontalLine.Label.Text =
                snappedY.ToString("N2");

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

            UpdateMouseInformation(
                new ScottPlot.Coordinates(snappedX, snappedY));

            Chart.Refresh();
        }

        private double GetNearestCandleX(double mouseX)
        {
            if (_bars.Count == 0)
                return mouseX;

            double bestX = GetBarX(0);
            double bestDistance = Math.Abs(bestX - mouseX);

            for (int i = 1; i < _bars.Count; i++)
            {
                double x = GetBarX(i);
                double distance = Math.Abs(x - mouseX);

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
            int index = GetNearestCandleIndex(x);

            if (!HasRealDates)
                return $"کندل {index}";

            try
            {
                DateTime dateTime = GetBarDateTime(_bars[index], index);

                return dateTime.TimeOfDay == TimeSpan.Zero
                    ? dateTime.ToString("yyyy/MM/dd")
                    : dateTime.ToString("yyyy/MM/dd HH:mm");
            }
            catch
            {
                return $"کندل {index}";
            }
        }
    }
}
