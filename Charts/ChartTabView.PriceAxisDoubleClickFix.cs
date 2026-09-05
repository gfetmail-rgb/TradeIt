using System;
using System.Windows;
using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _priceAxisDoubleClickFixAttached;

        private void AttachPriceAxisDoubleClickFix()
        {
            if (_priceAxisDoubleClickFixAttached)
                return;

            _priceAxisDoubleClickFixAttached = true;
            Chart.AddHandler(
                UIElement.PreviewMouseLeftButtonDownEvent,
                new System.Windows.Input.MouseButtonEventHandler(PriceAxisDoubleClickFix_MouseLeftButtonDown),
                true);
        }

        private void PriceAxisDoubleClickFix_MouseLeftButtonDown(object sender, WpfMouseButtonEventArgs e)
        {
            if (e.ChangedButton != System.Windows.Input.MouseButton.Left || e.ClickCount != 2)
                return;

            Point point = e.GetPosition(Chart);
            if (!IsPriceAxisPoint(point.X, point.Y))
                return;

            FitVisiblePriceRangeToPlot();
            e.Handled = true;
        }

        private bool IsPriceAxisPoint(double x, double y)
        {
            double width = Chart.ActualWidth;
            double height = Chart.ActualHeight;
            if (width <= 0 || height <= 0)
                return false;

            if (y >= height - 55.0)
                return false;

            return x <= 75.0 || x >= width - 30.0;
        }
    }
}
