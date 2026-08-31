using System;
using System.Windows;
using WpfPoint = System.Windows.Point;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using TradeIt.Models;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private void ChartOhlcOverlay_Loaded(object sender, RoutedEventArgs e)
        {
            if (ChartTitleTextBlock != null)
                ChartTitleTextBlock.Text = _symbol.DisplayName;

            // Both plots use exactly the same fixed data-area rectangle.
            // This prevents ScottPlot's automatic axis-label measurement from
            // shifting the volume plot horizontally relative to the candles.
            ScottPlot.PixelPadding sharedPadding =
                new ScottPlot.PixelPadding(75, 0, 30, 55);

            Chart.Plot.Layout.Fixed(sharedPadding);
            VolumeChart.Plot.Layout.Fixed(sharedPadding);

            VolumeChart.MouseMove += VolumeChart_MouseMoveForInfo;
            VolumeChart.MouseLeave += VolumeChart_MouseLeaveForInfo;

            UpdateChartOhlcHeader(_bars.Count > 0 ? _bars.Count - 1 : -1);
            UpdateVolumeInfo(_bars.Count > 0 ? _bars.Count - 1 : -1);
        }

        private void ChartPrintArea_MouseMove(object sender, WpfMouseEventArgs e)
        {
            try
            {
                if (_bars.Count == 0 || ChartOhlcTextBlock == null)
                    return;

                WpfPoint p = e.GetPosition(Chart);
                if (!TryGetChartCoordinates(Chart, p, out var coordinates))
                    return;

                int index = GetNearestCandleIndex(coordinates.X) - 1;
                UpdateChartOhlcHeader(index);
                UpdateVolumeInfo(index);
            }
            catch
            {
            }
        }

        private void VolumeChart_MouseMoveForInfo(object sender, WpfMouseEventArgs e)
        {
            try
            {
                if (_bars.Count == 0)
                    return;

                WpfPoint p = e.GetPosition(VolumeChart);
                if (!TryGetChartCoordinates(VolumeChart, p, out var coordinates))
                    return;

                int index = GetNearestCandleIndex(coordinates.X) - 1;
                UpdateVolumeInfo(index);
            }
            catch
            {
            }
        }

        private void UpdateChartOhlcHeader(int index)
        {
            if (ChartOhlcTextBlock == null || index < 0 || index >= _bars.Count)
                return;

            MarketBar bar = _bars[index];

            string xInfo;
            if (HasRealDates)
            {
                DateTime t = GetBarDateTime(bar, index);
                xInfo = t.TimeOfDay == TimeSpan.Zero
                    ? t.ToString("yyyy/MM/dd")
                    : t.ToString("yyyy/MM/dd HH:mm");
            }
            else
            {
                xInfo = $"کندل {index + 1}";
            }

            ChartOhlcTextBlock.Text =
                $"O: {bar.Open:N2}   H: {bar.High:N2}   L: {bar.Low:N2}   C: {bar.Close:N2}   |   {xInfo}";
        }

        private void UpdateVolumeInfo(int index)
        {
            if (VolumeInfoTextBlock == null || index < 0 || index >= _bars.Count)
                return;

            MarketBar bar = _bars[index];
            VolumeInfoTextBlock.Text =
                $"حجم: {bar.Volume / VolumeScale:N0}";
        }

        private void ChartPrintArea_MouseLeave(object sender, WpfMouseEventArgs e)
        {
            if (_bars.Count > 0)
            {
                int index = _bars.Count - 1;
                UpdateChartOhlcHeader(index);
                UpdateVolumeInfo(index);
            }
        }

        private void VolumeChart_MouseLeaveForInfo(object sender, WpfMouseEventArgs e)
        {
            if (_bars.Count > 0)
                UpdateVolumeInfo(_bars.Count - 1);
        }
    }
}
