using System;
using System.Windows;
using System.Windows.Input;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private void ChartPrintArea_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_bars.Count == 0 || ChartOhlcTextBlock == null)
                    return;

                Point p = e.GetPosition(Chart);
                if (!TryGetChartCoordinates(Chart, p, out var coordinates))
                    return;

                int index = GetNearestCandleIndex(coordinates.X) - 1;
                if (index < 0 || index >= _bars.Count)
                    return;

                MarketBar bar = _bars[index];
                string date = GetBarDateTime(bar, index).TimeOfDay == TimeSpan.Zero
                    ? GetBarDateTime(bar, index).ToString("yyyy/MM/dd")
                    : GetBarDateTime(bar, index).ToString("yyyy/MM/dd HH:mm");

                ChartOhlcTextBlock.Text =
                    $"Open: {bar.Open:N2}   High: {bar.High:N2}   Low: {bar.Low:N2}   Close: {bar.Close:N2}   حجم: {bar.Volume / VolumeScale:N0}   |   {date}";
            }
            catch
            {
                // The chart must remain usable even if an individual bar has invalid data.
            }
        }

        private void ChartPrintArea_MouseLeave(object sender, MouseEventArgs e)
        {
            if (ChartOhlcTextBlock != null)
                ChartOhlcTextBlock.Text = "";
        }
    }
}
