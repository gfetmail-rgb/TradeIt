using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private void ChartTabView_DrawingAttachLoaded(object sender, RoutedEventArgs e)
        {
            InitializeTechnicalDrawingHandling();
            InitializeTextDrawingHandling();
            AttachAdvancedDrawingTools();
            AttachUnifiedDrawingInput();
            InitializeDrawingCursorHandling();
        }
    }
}
