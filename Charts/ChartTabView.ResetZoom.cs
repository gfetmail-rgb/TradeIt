using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_hasInitialView) return;

            Chart.Plot.Axes.SetLimits(
                _initialXMin,
                _initialXMax,
                _initialYMin,
                _initialYMax);

            RenderAllFibonacciDrawings();
            Chart.Refresh();
        }
    }
}
