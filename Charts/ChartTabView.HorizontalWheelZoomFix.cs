using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ScottPlot.WPF;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _horizontalWheelZoomRegistered = RegisterHorizontalWheelZoom();

        private static bool RegisterHorizontalWheelZoom()
        {
            EventManager.RegisterClassHandler(
                typeof(WpfPlot),
                UIElement.PreviewMouseWheelEvent,
                new MouseWheelEventHandler(HorizontalWheelZoom_ClassHandler));
            return true;
        }

        private static void HorizontalWheelZoom_ClassHandler(object sender, MouseWheelEventArgs e)
        {
            if (sender is not WpfPlot plot || e.Handled)
                return;

            ChartTabView? chart = FindChartTabView(plot);
            if (chart == null)
                return;

            chart.ZoomXAxisFromWheel(e.Delta > 0 ? 0.80 : 1.25);
            e.Handled = true;
        }

        private static ChartTabView? FindChartTabView(DependencyObject start)
        {
            DependencyObject? current = start;
            while (current != null)
            {
                if (current is ChartTabView chart)
                    return chart;

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private void ZoomXAxisFromWheel(double factor)
        {
            if (!_hasInitialView)
                return;

            ScottPlot.AxisLimits limits = Chart.Plot.Axes.GetLimits();
            double range = limits.Right - limits.Left;
            if (!double.IsFinite(range) || range <= 0)
                return;

            double initialRange = _initialXMax - _initialXMin;
            if (!double.IsFinite(initialRange) || initialRange <= 0)
                initialRange = range;

            double newRange = range * factor;
            newRange = Math.Max(initialRange / 10000.0, Math.Min(initialRange * 2.0, newRange));

            // Horizontal zoom is anchored to the current right edge.
            // Only the left limit moves, so the latest candle stays fixed.
            double right = limits.Right;
            double left = right - newRange;

            Chart.Plot.Axes.SetLimitsX(left, right);
            Chart.Refresh();
        }
    }
}
