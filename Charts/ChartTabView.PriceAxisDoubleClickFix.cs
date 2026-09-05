using System;
using TradeIt.Models;
using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _priceAxisDoubleClickFixAttached;
        private static readonly bool _priceAxisDoubleClickFixRegistered = RegisterPriceAxisDoubleClickFix();

        private static bool RegisterPriceAxisDoubleClickFix()
        {
            System.Windows.EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                System.Windows.FrameworkElement.LoadedEvent,
                new System.Windows.RoutedEventHandler(PriceAxisDoubleClickFix_Loaded));
            return true;
        }

        private static void PriceAxisDoubleClickFix_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.AttachPriceAxisDoubleClickFix();
        }

        private void AttachPriceAxisDoubleClickFix()
        {
            if (_priceAxisDoubleClickFixAttached)
                return;

            _priceAxisDoubleClickFixAttached = true;
            Chart.AddHandler(
                System.Windows.UIElement.PreviewMouseLeftButtonDownEvent,
                new System.Windows.Input.MouseButtonEventHandler(PriceAxisDoubleClickFix_MouseLeftButtonDown),
                true);
        }

        private void PriceAxisDoubleClickFix_MouseLeftButtonDown(object sender, WpfMouseButtonEventArgs e)
        {
            if (e.ChangedButton != System.Windows.Input.MouseButton.Left || e.ClickCount != 2)
                return;

            System.Windows.Point point = e.GetPosition(Chart);
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

        private void FitVisiblePriceRangeToPlot()
        {
            if (_bars.Count == 0)
                return;

            var limits = Chart.Plot.Axes.GetLimits();
            double minPrice = double.MaxValue;
            double maxPrice = double.MinValue;

            for (int i = 0; i < _bars.Count; i++)
            {
                MarketBar bar = _bars[i];
                double x = GetBarDateTime(bar, i).ToOADate();
                if (!double.IsFinite(x) || x < limits.Left || x > limits.Right)
                    continue;

                minPrice = Math.Min(minPrice, bar.Low);
                maxPrice = Math.Max(maxPrice, bar.High);
            }

            if (minPrice == double.MaxValue || maxPrice == double.MinValue)
                return;

            double range = maxPrice - minPrice;
            double padding = range > 0
                ? range * 0.05
                : Math.Max(Math.Abs(maxPrice) * 0.01, 1.0);

            Chart.Plot.Axes.SetLimits(
                limits.Left,
                limits.Right,
                minPrice - padding,
                maxPrice + padding);
            Chart.Refresh();
        }
    }
}
