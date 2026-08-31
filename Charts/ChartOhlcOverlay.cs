using System;
using System.Windows;
using System.Windows.Input;
using TradeIt.Models;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private void ChartOhlcOverlay_Loaded(object sender, RoutedEventArgs e)
        {
            // Layout must not be changed here. Doing so after the first render
            // causes a visible jump when the mouse first enters the chart.
            if (ChartTitleTextBlock != null)
                ChartTitleTextBlock.Text = _symbol.DisplayName;

            UpdateChartOhlcHeader(_bars.Count > 0 ? _bars.Count - 1 : -1);
            UpdateVolumeInfo(_bars.Count > 0 ? _bars.Count - 1 : -1);
        }

        private void ChartPrintArea_MouseMove(object sender, WpfMouseEventArgs e)
        {
            try
            {
                if (_bars.Count == 0 || ChartOhlcTextBlock == null)
                    return;

                var p = e.GetPosition(Chart);
                if (!TryGetChartCoordinates(Chart, p, out var coordinates))
                    return;

                int index = GetNearestCandleIndex(coordinates.X) - 1;
                UpdateChartOhlcHeader(index);
                UpdateVolumeInfo(index);
            }
            catch { }
        }

        private void VolumeChart_MouseMoveForInfo(object sender, WpfMouseEventArgs e)
        {
            try
            {
                if (_bars.Count == 0)
                    return;

                var p = e.GetPosition(VolumeChart);
                if (!TryGetChartCoordinates(VolumeChart, p, out var coordinates))
                    return;

                int index = GetNearestCandleIndex(coordinates.X) - 1;
                UpdateVolumeInfo(index);
            }
            catch { }
        }

        private void UpdateChartOhlcHeader(int index)
        {
            if (ChartOhlcTextBlock == null || index < 0 || index >= _bars.Count)
                return;

            MarketBar bar = _bars[index];
            string xInfo = $"کندل {index + 1}";
            if (HasRealDates)
            {
                DateTime t = GetBarDateTime(bar, index);
                xInfo = t.TimeOfDay == TimeSpan.Zero
                    ? t.ToString("yyyy/MM/dd")
                    : t.ToString("yyyy/MM/dd HH:mm");
            }

            ChartOhlcTextBlock.Text =
                $"O: {bar.Open:N2}   H: {bar.High:N2}   L: {bar.Low:N2}   C: {bar.Close:N2}   |   {xInfo}";
        }

        private void UpdateVolumeInfo(int index)
        {
            if (VolumeInfoTextBlock == null || index < 0 || index >= _bars.Count)
                return;

            VolumeInfoTextBlock.Text = $"حجم: {_bars[index].Volume / VolumeScale:N0}";
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
