using System.Windows;
using System.Windows.Input;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _drawingInputRoutingFixAttached;
        private static readonly bool _drawingInputRoutingFixRegistered = RegisterDrawingInputRoutingFix();

        private static bool RegisterDrawingInputRoutingFix()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(DrawingInputRoutingFix_Loaded));
            return true;
        }

        private static void DrawingInputRoutingFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.AttachDrawingInputRoutingFix();
        }

        private void AttachDrawingInputRoutingFix()
        {
            if (_drawingInputRoutingFixAttached) return;
            _drawingInputRoutingFixAttached = true;
        }
    }
}
