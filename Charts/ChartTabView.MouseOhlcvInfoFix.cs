using TradeIt.Models;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfMouseEventHandler = System.Windows.Input.MouseEventHandler;
using WpfPoint = System.Windows.Point;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _mouseOhlcvInfoFixAttached;
        private static readonly bool _mouseOhlcvInfoFixRegistered = RegisterMouseOhlcvInfoFix();

        private static bool RegisterMouseOhlcvInfoFix()
        {
            System.Windows.EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                System.Windows.FrameworkElement.LoadedEvent,
                new System.Windows.RoutedEventHandler(MouseOhlcvInfoFix_Loaded));
            return true;
        }

        private static void MouseOhlcvInfoFix_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.AttachMouseOhlcvInfoFix();
        }

        private void AttachMouseOhlcvInfoFix()
        {
            if (_mouseOhlcvInfoFixAttached)
                return;

            _mouseOhlcvInfoFixAttached = true;

            // Bubble MouseMove runs after Chart_PreviewMouseMove and after
            // ScottPlot's preview processing. handledEventsToo guarantees that
            // this final handler still runs if ScottPlot marked the event handled.
            Chart.AddHandler(
                System.Windows.UIElement.MouseMoveEvent,
                new WpfMouseEventHandler(MouseOhlcvInfoFix_MouseMove),
                true);
        }

        private void MouseOhlcvInfoFix_MouseMove(object sender, WpfMouseEventArgs e)
        {
            if (!_chartVisible || _bars.Count == 0 || _continuousTimeAxisApplied)
                return;

            WpfPoint mousePosition = e.GetPosition(Chart);
            if (!TryGetChartCoordinates(Chart, mousePosition, out ScottPlot.Coordinates coordinates))
                return;

            int barIndex = FindNearestBarIndex(coordinates.X);
            if (barIndex < 0 || barIndex >= _bars.Count)
                return;

            // FinalChartFixes owns the final text format and writes it later in
            // the same route. Keep this handler as a robust fallback only.
            UpdateOHLCVInfo(barIndex);
        }
    }
}
