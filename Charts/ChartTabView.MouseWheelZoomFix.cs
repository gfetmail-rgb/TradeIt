using System;
using System.Windows;
using System.Windows.Input;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _mouseWheelZoomFixAttached;
        private static readonly bool _mouseWheelZoomFixRegistered = RegisterMouseWheelZoomFix();

        private static bool RegisterMouseWheelZoomFix()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(MouseWheelZoomFix_Loaded));
            return true;
        }

        private static void MouseWheelZoomFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.AttachMouseWheelZoomFix();
        }

        private void AttachMouseWheelZoomFix()
        {
            if (_mouseWheelZoomFixAttached)
                return;

            _mouseWheelZoomFixAttached = true;
            Chart.AddHandler(
                UIElement.PreviewMouseWheelEvent,
                new System.Windows.Input.MouseWheelEventHandler(MouseWheelZoomFix_MouseWheel),
                true);
        }

        private void MouseWheelZoomFix_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (e.Delta == 0)
                return;

            var limits = Chart.Plot.Axes.GetLimits();
            double range = limits.Right - limits.Left;
            if (!double.IsFinite(range) || range <= 0)
                return;

            // Wheel zoom is horizontal only. The right edge is the fixed anchor;
            // therefore the visible chart expands/contracts exclusively toward
            // the left when zooming out/in.
            double factor = e.Delta > 0 ? 0.80 : 1.25;
            double newRange = range * factor;

            double initialRange = _initialXMax - _initialXMin;
            if (initialRange > 0 && double.IsFinite(initialRange))
            {
                newRange = Math.Max(initialRange / 10000.0,
                    Math.Min(initialRange * 2.0, newRange));
            }
            else
            {
                newRange = Math.Max(range / 10000.0, newRange);
            }

            Chart.Plot.Axes.SetLimits(
                limits.Right - newRange,
                limits.Right,
                limits.Bottom,
                limits.Top);

            Chart.Refresh();
            e.Handled = true;
        }
    }
}
