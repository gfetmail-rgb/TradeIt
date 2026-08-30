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
                // When the source data has no real dates, never fabricate
                // a date. Display the actual candle number instead.
                xInfo = $"کندل {index + 1}";
            }

            ChartOhlcTextBlock.Text =
                $"O: {bar.Open:N2}   H: {bar.High:N2}   L: {bar.Low:N2}   C: {bar.Close:N2}   V: {bar.Volume / VolumeScale:N0}   |   {xInfo}";
        }

        private void ChartPrintArea_MouseLeave(object sender, WpfMouseEventArgs e)
        {
            if (_bars.Count > 0)
                UpdateChartOhlcHeader(_bars.Count - 1);
        }
    }
}
