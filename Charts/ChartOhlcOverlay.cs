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

            UpdateChartOhlcHeader(_bars.Count > 0 ? _bars.Count - 1 : -1);
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
            DateTime t = GetBarDateTime(bar, index);
            string date = t.TimeOfDay == TimeSpan.Zero
                ? t.ToString("yyyy/MM/dd")
                : t.ToString("yyyy/MM/dd HH:mm");

            ChartOhlcTextBlock.Text =
                $"Open: {bar.Open:N2}   High: {bar.High:N2}   Low: {bar.Low:N2}   Close: {bar.Close:N2}   حجم: {bar.Volume / VolumeScale:N0}   |   {date}";
        }

        private void ChartPrintArea_MouseLeave(object sender, WpfMouseEventArgs e)
        {
            if (_bars.Count > 0)
                UpdateChartOhlcHeader(_bars.Count - 1);
        }
    }
}
