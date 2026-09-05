using System.Windows;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _mouseOhlcvInfoFixAttached;
        private static readonly bool _mouseOhlcvInfoFixRegistered = RegisterMouseOhlcvInfoFix();

        private static bool RegisterMouseOhlcvInfoFix()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(MouseOhlcvInfoFix_Loaded));
            return true;
        }

        private static void MouseOhlcvInfoFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.AttachMouseOhlcvInfoFix();
        }

        private void AttachMouseOhlcvInfoFix()
        {
            if (_mouseOhlcvInfoFixAttached)
                return;

            _mouseOhlcvInfoFixAttached = true;
            Chart.MouseMove += MouseOhlcvInfoFix_MouseMove;
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

            ChartInfoTextBlock.Text =
                $"{_symbol.Symbol} | تاریخ: {dateText} | ساعت: {timeText} | " +
                $"O: {bar.Open:N2} | H: {bar.High:N2} | L: {bar.Low:N2} | C: {bar.Close:N2} | V: {bar.Volume:N0}";
        }
    }
}
