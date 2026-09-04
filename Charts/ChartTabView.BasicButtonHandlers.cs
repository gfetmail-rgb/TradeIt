using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private void HideChartButton_Click(object sender, RoutedEventArgs e)
        {
            _chartVisible = !_chartVisible;

            foreach (var plottable in Chart.Plot.GetPlottables())
            {
                if (ReferenceEquals(plottable, _crosshair))
                    continue;

                plottable.IsVisible = _chartVisible;
            }

            if (_crosshair != null)
                _crosshair.IsVisible = _chartVisible && _crosshairVisible && (_crosshairMouseInside || !_hasInitialView);

            HideChartButton.Content = _chartVisible ? "پنهان کردن نمودار" : "نمایش نمودار";
            Chart.Refresh();
        }

        private void HideToolsButton_Click(object sender, RoutedEventArgs e)
        {
            _toolsVisible = !_toolsVisible;
            TechnicalDrawingToolbarHost.Visibility = _toolsVisible
                ? Visibility.Visible
                : Visibility.Collapsed;

            HideToolsButton.Content = _toolsVisible
                ? "پنهان کردن ابزارهای تکنیکال"
                : "نمایش ابزارهای تکنیکال";
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomXAxis(0.80);
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomXAxis(1.25);
        }
    }
}
