using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private void ChartTabView_DrawingAttachLoaded(object sender, RoutedEventArgs e)
        {
            AttachUnifiedDrawingInput();
            UnifiedDrawing_Loaded(sender, e);

            // Attach the ruler through the same explicit Loaded pipeline used by
            // the other drawing tools. This guarantees its mouse handlers are
            // connected before the user can activate it.
            AttachMeasurementToolHandling();
        }
    }
}
