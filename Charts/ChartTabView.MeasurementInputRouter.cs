using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ScottPlot.WPF;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _measurementInputRouterRegistered = RegisterMeasurementInputRouter();

        private static bool RegisterMeasurementInputRouter()
        {
            // WpfPlot is the actual input target. Its class handler runs before
            // ChartTabView's instance PreviewMouse handlers, so the ruler gets
            // first ownership of the click and mouse-move events.
            EventManager.RegisterClassHandler(
                typeof(WpfPlot),
                UIElement.PreviewMouseLeftButtonDownEvent,
                new System.Windows.Input.MouseButtonEventHandler(MeasurementInputRouter_MouseDown),
                true);

            EventManager.RegisterClassHandler(
                typeof(WpfPlot),
                UIElement.PreviewMouseMoveEvent,
                new System.Windows.Input.MouseEventHandler(MeasurementInputRouter_MouseMove),
                true);

            return true;
        }

        private static ChartTabView? GetChartTabView(WpfPlot plot)
        {
            DependencyObject? current = plot;
            while (current != null)
            {
                if (current is ChartTabView chart)
                    return chart;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private static void MeasurementInputRouter_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not WpfPlot plot)
                return;

            ChartTabView? chart = GetChartTabView(plot);
            if (chart == null || (int)chart._activeDrawingTool != MeasurementToolValue || e.ChangedButton != MouseButton.Left)
                return;

            chart.MeasurementTool_MouseDown(plot, e);
            e.Handled = true;
        }

        private static void MeasurementInputRouter_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is not WpfPlot plot)
                return;

            ChartTabView? chart = GetChartTabView(plot);
            if (chart == null || (int)chart._activeDrawingTool != MeasurementToolValue)
                return;

            chart.MeasurementTool_MouseMove(plot, e);
            e.Handled = true;
        }
    }
}
