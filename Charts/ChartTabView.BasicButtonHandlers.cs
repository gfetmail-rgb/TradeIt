using System;
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

        private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_hasInitialView)
                return;

            Chart.Plot.Axes.SetLimits(
                _initialXMin,
                _initialXMax,
                _initialYMin,
                _initialYMax);

            RenderAllFibonacciDrawings();
            Chart.Refresh();
        }

        private void ScreenshotChartOnlyButton_Click(object sender, RoutedEventArgs e)
        {
            ScreenshotButton_Click(sender, e);
        }

        private void PrintChartOnlyButton_Click(object sender, RoutedEventArgs e)
        {
            PrintButton_Click(sender, e);
        }
    }
}
