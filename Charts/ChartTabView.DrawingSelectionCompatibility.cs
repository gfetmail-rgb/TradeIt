using System.Windows;
using System.Windows.Controls;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        // Compatibility entry points retained after consolidating the drawing modules.
        // The actual technical-tool state is maintained by UpdateTechnicalDrawingButtons().
        private void SetAllDrawingButtonVisuals()
        {
            UpdateTechnicalDrawingButtons();
        }

        private void UpdateFibonacciButtonVisualState()
        {
            UpdateTechnicalDrawingButtons();
        }
    }
}
