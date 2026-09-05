using TradeIt.Models;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfMouseEventHandler = System.Windows.Input.MouseEventHandler;
using WpfPoint = System.Windows.Point;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _mouseOhlcvInfoFixAttached;
        private int _mouseOhlcvInfoFixGeneration;
        private static readonly bool _mouseOhlcvInfoFixRegistered = RegisterMouseOhlcvInfoFix();

        private static bool RegisterMouseOhlcvInfoFix()
        {
            System.Windows.EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                System.Windows.FrameworkElement.LoadedEvent,
                new System.Windows.RoutedEventHandler(MouseOhlcvInfoFix_Loaded));

            System.Windows.EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                System.Windows.UIElement.PreviewMouseMoveEvent,
                new WpfMouseEventHandler(MouseOhlcvInfoFix_ClassMouseMove),
                true);
            return true;
        }

        private static void MouseOhlcvInfoFix_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.AttachMouseOhlcvInfoFix();
        }

        private static void MouseOhlcvInfoFix_ClassMouseMove(object sender, WpfMouseEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.MouseOhlcvInfoFix_MouseMove(chart.Chart, e);
        }

        private void AttachMouseOhlcvInfoFix()
        {
            if (_mouseOhlcvInfoFixAttached)
                return;

            _mouseOhlcvInfoFixAttached = true;
        }

        private void MouseOhlcvInfoFix_MouseMove(object sender, WpfMouseEventArgs e)
        {
            if (!_chartVisible || _bars.Count == 0)
                return;

            WpfPoint mousePosition = e.GetPosition(Chart);
            if (!TryGetChartCoordinates(Chart, mousePosition, out ScottPlot.Coordinates coordinates))
                return;

            int barIndex = FindNearestBarIndex(coordinates.X);
            if (barIndex < 0 || barIndex >= _bars.Count)
                return;

            MarketBar bar = _bars[barIndex];
            string dateText = GetSourceDateLabel(barIndex);
            if (string.IsNullOrWhiteSpace(dateText))
                dateText = GetBarDateTime(bar, barIndex).ToString("yyyy/MM/dd");

            string timeText = bar.Time ?? string.Empty;
            if (string.IsNullOrWhiteSpace(timeText))
                timeText = GetBarDateTime(bar, barIndex).ToString("HH:mm:ss");

            string infoText =
                $"{_symbol.Symbol} | تاریخ: {dateText} | ساعت: {timeText} | " +
                $"O: {bar.Open:N2} | H: {bar.High:N2} | L: {bar.Low:N2} | C: {bar.Close:N2} | V: {bar.Volume:N0}";

            // Chart_PreviewMouseMove and ScottPlot can update the same TextBlock
            // later in the routed event. Schedule the OHLCV text after the current
            // input route so it is the final value visible to the user.
            int generation = ++_mouseOhlcvInfoFixGeneration;
            Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Input,
                new System.Action(() =>
                {
                    if (generation == _mouseOhlcvInfoFixGeneration && _chartVisible)
                        ChartInfoTextBlock.Text = infoText;
                }));
        }
    }
}
