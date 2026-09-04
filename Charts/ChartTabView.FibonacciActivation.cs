using System.Windows;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfSystemColors = System.Windows.SystemColors;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _fibonacciActivationRegistered = RegisterFibonacciActivationHandling();

        private static bool RegisterFibonacciActivationHandling()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(FibonacciActivation_Loaded));
            return true;
        }

        private static void FibonacciActivation_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart)
                return;

            chart.AttachUnifiedDrawingInput();
            chart.UpdateFibonacciButtonVisualState();
        }

        private void UpdateFibonacciButtonVisualState()
        {
            bool retracementActive = (int)_activeDrawingTool == UnifiedFibRetracement;
            bool extensionActive = (int)_activeDrawingTool == UnifiedFibExtension;

            SetDrawingToolButtonState(DrawingFibRetracementButton, retracementActive);
            SetDrawingToolButtonState(DrawingFibExtensionButton, extensionActive);
        }

        private static void SetDrawingToolButtonState(System.Windows.Controls.Button button, bool active)
        {
            button.Opacity = active ? 1.0 : 0.55;
            button.BorderThickness = active ? new Thickness(2) : new Thickness(1);
            button.BorderBrush = active
                ? WpfBrushes.DodgerBlue
                : WpfSystemColors.ControlDarkBrush;
            button.FontWeight = active ? FontWeights.Bold : FontWeights.Normal;
        }
    }
}
