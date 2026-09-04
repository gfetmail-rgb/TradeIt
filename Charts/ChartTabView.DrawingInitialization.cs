using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _drawingToolsRegistered = RegisterDrawingToolsHandling();

        private static bool RegisterDrawingToolsHandling()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(DrawingTools_Loaded));
            return true;
        }

        private static void DrawingTools_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart)
                return;

            chart.InitializeTechnicalDrawingHandling();
            chart.InitializeTextDrawingHandling();
            chart.AttachAdvancedDrawingTools();
            chart.AttachUnifiedDrawingInput();
            chart.InitializeDrawingCursorHandling();
        }
    }
}
