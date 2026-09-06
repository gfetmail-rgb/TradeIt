using System;
using System.Windows;
using System.Windows.Input;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _coordinateConsistencyFixAttached;
        private static readonly bool _coordinateConsistencyFixRegistered = RegisterCoordinateConsistencyFix();

        private static bool RegisterCoordinateConsistencyFix()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(CoordinateConsistencyFix_Loaded));
            return true;
        }

        private static void CoordinateConsistencyFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart || chart._coordinateConsistencyFixAttached)
                return;

            chart._coordinateConsistencyFixAttached = true;
            chart.Chart.AddHandler(
                UIElement.PreviewMouseMoveEvent,
                new System.Windows.Input.MouseEventHandler(chart.CoordinateConsistencyFix_MouseMove),
                true);
            chart.Chart.AddHandler(
                UIElement.PreviewMouseLeftButtonDownEvent,
                new System.Windows.Input.MouseButtonEventHandler(chart.CoordinateConsistencyFix_MouseLeftButtonDown),
                true);
        }

        private void CoordinateConsistencyFix_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_continuousTimeAxisApplied || !_chartVisible || !_crosshairVisible || _crosshair == null)
                return;

            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates coordinates))
                return;

            int index = FindNearestContinuousBarIndex(coordinates.X);
            if (index < 0)
                return;

            _crosshair.Position = new ScottPlot.Coordinates(ContinuousX(index), coordinates.Y);
            _crosshair.HorizontalLine.Text = coordinates.Y.ToString("N2");
            _crosshair.IsVisible = true;
            _crosshairMouseInside = true;
            UpdateCrosshairAxisLabel(index);
            UpdateMouseInformation(coordinates, index);
        }

        private int FindNearestContinuousBarIndex(double x)
        {
            if (_bars.Count == 0 || double.IsNaN(x) || double.IsInfinity(x))
                return -1;

            int low = 0;
            int high = _bars.Count - 1;

            while (low <= high)
            {
                int mid = low + ((high - low) >> 1);
                double midX = ContinuousX(mid);
                if (midX < x)
                    low = mid + 1;
                else if (midX > x)
                    high = mid - 1;
                else
                    return mid;
            }

            if (low <= 0) return 0;
            if (low >= _bars.Count) return _bars.Count - 1;
            return Math.Abs(ContinuousX(low) - x) < Math.Abs(x - ContinuousX(low - 1))
                ? low
                : low - 1;
        }

        private void CoordinateConsistencyFix_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_continuousTimeAxisApplied || e.ChangedButton != MouseButton.Left || e.ClickCount != 2)
                return;

            System.Windows.Point point = e.GetPosition(Chart);
            if (point.X < 0 || point.X > Chart.ActualWidth)
                return;

            double width = Chart.ActualWidth;
            double height = Chart.ActualHeight;
            if (width <= 0 || height <= 0)
                return;

            const double leftAxisWidth = 75.0;
            const double rightAxisWidth = 30.0;
            const double bottomAxisHeight = 55.0;
            bool onPriceAxis = point.Y < height - bottomAxisHeight &&
                               (point.X <= leftAxisWidth || point.X >= width - rightAxisWidth);
            if (!onPriceAxis)
                return;

            AutoFitVisiblePriceRangeContinuous();
            e.Handled = true;
        }

        private void AutoFitVisiblePriceRangeContinuous()
        {
            if (!_hasInitialView || _bars.Count == 0)
                return;

            var limits = Chart.Plot.Axes.GetLimits();
            double minPrice = double.MaxValue;
            double maxPrice = double.MinValue;

            int first = Math.Max(0, (int)Math.Ceiling(limits.Left - 2000.0));
            int last = Math.Min(_bars.Count - 1, (int)Math.Floor(limits.Right - 2000.0));
            if (last < first)
                return;

            for (int i = first; i <= last; i++)
            {
                minPrice = Math.Min(minPrice, _bars[i].Low);
                maxPrice = Math.Max(maxPrice, _bars[i].High);
            }

            if (minPrice == double.MaxValue || maxPrice == double.MinValue)
                return;

            double range = maxPrice - minPrice;
            double padding = range > 0
                ? range * 0.05
                : Math.Max(Math.Abs(maxPrice) * 0.01, 1);

            Chart.Plot.Axes.SetLimits(
                limits.Left,
                limits.Right,
                minPrice - padding,
                maxPrice + padding);
            Chart.Refresh();
        }
    }
}
