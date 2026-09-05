using System.Windows;
using WpfMouseWheelEventArgs = System.Windows.Input.MouseWheelEventArgs;
using WpfMouseWheelEventHandler = System.Windows.Input.MouseWheelEventHandler;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _mouseWheelRoutingFixRegistered = RegisterMouseWheelRoutingFix();

        private static bool RegisterMouseWheelRoutingFix()
        {
            // The original Chart_PreviewMouseWheel handler and the dedicated
            // MouseWheelZoomFix both receive the same wheel event. The latter
            // listens with handledEventsToo=true. Mark the event handled at the
            // ChartTabView level first, so the original instance handler on Chart
            // does not perform a second zoom. The dedicated fix then performs
            // exactly one horizontal zoom while keeping the right edge fixed.
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                UIElement.PreviewMouseWheelEvent,
                new WpfMouseWheelEventHandler(MouseWheelRoutingFix_PreviewMouseWheel),
                false);
            return true;
        }

        private static void MouseWheelRoutingFix_PreviewMouseWheel(object sender, WpfMouseWheelEventArgs e)
        {
            if (sender is not ChartTabView chart || chart.Chart == null)
                return;

            DependencyObject? source = e.OriginalSource as DependencyObject;
            if (!chart.IsSourceInsideChart(source))
                return;

            e.Handled = true;
        }

        private bool IsSourceInsideChart(DependencyObject? source)
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
