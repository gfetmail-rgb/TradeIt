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
    }
}
