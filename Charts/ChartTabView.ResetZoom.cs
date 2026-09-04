using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset must restore the same canonical initial view used when the
            // chart was opened. Do not redraw/remove plottables here.
            ApplyInitialCandleRange();
            Chart.Refresh();
        }
    }
}
