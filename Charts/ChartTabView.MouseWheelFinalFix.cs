using System;
using System.Windows;
using WpfMouseWheelEventArgs = System.Windows.Input.MouseWheelEventArgs;
using WpfMouseWheelEventHandler = System.Windows.Input.MouseWheelEventHandler;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _mouseWheelFinalFixAttached;
        private static readonly bool _mouseWheelFinalFixRegistered = RegisterMouseWheelFinalFix();

        private static bool RegisterMouseWheelFinalFix()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(MouseWheelFinalFix_Loaded));
            return true;
        }

        private static void MouseWheelFinalFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.AttachMouseWheelFinalFix();
        }

        private void AttachMouseWheelFinalFix()
        {
            if (_mouseWheelFinalFixAttached)
                return;

            _mouseWheelFinalFixAttached = true;

            // Stop the normal Chart_PreviewMouseWheel instance handler from also
            // zooming. The dedicated Chart handler below still runs because it
            // explicitly listens to handled events.
            AddHandler(
                UIElement.PreviewMouseWheelEvent,
                new WpfMouseWheelEventHandler(MouseWheelFinalFix_BlockDuplicateZoom),
                false);

            Chart.AddHandler(
                UIElement.PreviewMouseWheelEvent,
                new WpfMouseWheelEventHandler(MouseWheelFinalFix_ChartWheel),
                true);
        }

        private void MouseWheelFinalFix_BlockDuplicateZoom(object sender, WpfMouseWheelEventArgs e)
        {
            if (!IsMouseWheelEventInsideChart(e.OriginalSource as DependencyObject))
                return;

            e.Handled = true;
        }

        private void MouseWheelFinalFix_ChartWheel(object sender, WpfMouseWheelEventArgs e)
        {
            if (e.Delta == 0 || !IsMouseWheelEventInsideChart(e.OriginalSource as DependencyObject))
                return;

            var limits = Chart.Plot.Axes.GetLimits();
            double range = limits.Right - limits.Left;
            if (!double.IsFinite(range) || range <= 0)
                return;

            double initialRange = _initialXMax - _initialXMin;
            if (!double.IsFinite(initialRange) || initialRange <= 0)
                initialRange = range;

            double factor = e.Delta > 0 ? 0.80 : 1.25;
            double newRange = range * factor;
            newRange = Math.Max(
                initialRange / 10000.0,
                Math.Min(initialRange * 2.0, newRange));

            // The right edge is deliberately preserved. Therefore wheel zoom
            // changes only the left side of the visible time range.
            Chart.Plot.Axes.SetLimits(
                limits.Right - newRange,
                limits.Right,
                limits.Bottom,
                limits.Top);

            Chart.Refresh();
            e.Handled = true;
        }

        private bool IsMouseWheelEventInsideChart(DependencyObject? source)
        {
            DependencyObject? current = source;
            while (current != null)
            {
                if (ReferenceEquals(current, Chart))
                    return true;

                current = current is System.Windows.Media.Visual visual
                    ? System.Windows.Media.VisualTreeHelper.GetParent(visual)
                    : current is FrameworkElement element
                        ? element.Parent
                        : current is FrameworkContentElement content
                            ? content.Parent
                            : null;
            }

            return false;
        }
    }
}
