using System;
using System.Windows;
using System.Windows.Input;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _arrowAndPriceAxisInputFixAttached;
        private static readonly bool _arrowAndPriceAxisInputFixRegistered = RegisterArrowAndPriceAxisInputFix();

        private static bool RegisterArrowAndPriceAxisInputFix()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(ArrowAndPriceAxisInputFix_Loaded));
            return true;
        }

        private static void ArrowAndPriceAxisInputFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.AttachArrowAndPriceAxisInputFix();
        }

        private void AttachArrowAndPriceAxisInputFix()
        {
            if (_arrowAndPriceAxisInputFixAttached)
                return;

            _arrowAndPriceAxisInputFixAttached = true;

            Chart.AddHandler(
                UIElement.PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler(ArrowAndPriceAxisInputFix_MouseDown),
                true);
        }

        private void ArrowAndPriceAxisInputFix_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (_arrowDrawingActive && (int)_activeDrawingTool == 10)
            {
                ArrowDrawing_MouseDown(sender, e);
                return;
            }

            if (e.ClickCount == 2)
            {
                System.Windows.Point p = e.GetPosition(Chart);
                if (IsPriceAxisPoint(p.X, p.Y))
                {
                    AutoFitVisiblePriceRangeFixed();
                    e.Handled = true;
                }
            }
        }

        private bool IsPriceAxisPoint(double x, double y)
        {
            double width = Chart.ActualWidth;
            double height = Chart.ActualHeight;
            if (width <= 0 || height <= 0)
                return false;

            const double leftAxisWidth = 75.0;
            const double rightAxisWidth = 30.0;
            const double bottomAxisHeight = 55.0;

            if (y >= height - bottomAxisHeight)
                return false;

            return x <= leftAxisWidth || x >= width - rightAxisWidth;
        }

        private void AutoFitVisiblePriceRangeFixed()
        {
            if (_bars.Count == 0)
                return;

            ScottPlot.AxisLimits limits = Chart.Plot.Axes.GetLimits();
            double minPrice = double.MaxValue;
            double maxPrice = double.MinValue;

            for (int i = 0; i < _bars.Count; i++)
            {
                double x = _continuousTimeAxisApplied
                    ? 2000.0 + i
                    : GetBarDateTime(_bars[i], i).ToOADate();

                if (x < limits.Left || x > limits.Right)
                    continue;

                if (double.IsFinite(_bars[i].Low))
                    minPrice = Math.Min(minPrice, _bars[i].Low);
                if (double.IsFinite(_bars[i].High))
                    maxPrice = Math.Max(maxPrice, _bars[i].High);
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
