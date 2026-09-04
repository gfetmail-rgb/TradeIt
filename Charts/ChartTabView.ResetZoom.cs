using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset only changes axis limits. It must never redraw or remove plottables.
            if (!_hasInitialView)
            {
                ApplyInitialCandleRange();
                return;
            }

            Chart.Plot.Axes.SetLimits(
                _initialXMin,
                _initialXMax,
                _initialYMin,
                _initialYMax);
            Chart.Refresh();
        }
    }
}
