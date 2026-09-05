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
            InputManager.Current.PreProcessInput += DrawingInputRoutingFix_PreProcessInput;
        }

        private void DrawingInputRoutingFix_PreProcessInput(object sender, PreProcessInputEventArgs e)
        {
            if (e.StagingItem.Input is MouseButtonEventArgs mouseButton &&
                mouseButton.ChangedButton == MouseButton.Left &&
                SourceBelongsToThisChart(mouseButton.Source as DependencyObject) &&
                !mouseButton.Handled &&
                !_textDrawingActive)
            {
                int tool = (int)_activeDrawingTool;

                if (tool == 10 || IsUnifiedFibonacciActive() || IsAdvancedDrawingTool)
                {
                    if (tool == 10)
                        ArrowDrawing_MouseDown(Chart, mouseButton);
                    else if (IsUnifiedFibonacciActive())
                        UnifiedFib_MouseDown(mouseButton);
                    else
                        AdvancedDrawing_MouseDown(Chart, mouseButton);

                    mouseButton.Handled = true;
                    return;
                }
            }

            if (e.StagingItem.Input is MouseEventArgs mouseMove &&
                SourceBelongsToThisChart(mouseMove.Source as DependencyObject) &&
                !mouseMove.Handled &&
                !_textDrawingActive)
            {
                int tool = (int)_activeDrawingTool;

                if (tool == 10 || IsUnifiedFibonacciActive() || IsAdvancedDrawingTool)
                {
                    if (tool == 10)
                        ArrowDrawing_MouseMove(Chart, mouseMove);
                    else if (IsUnifiedFibonacciActive())
                        UnifiedFib_MouseMove(mouseMove);
                    else
                        AdvancedDrawing_MouseMove(Chart, mouseMove);

                    mouseMove.Handled = true;
                }
            }
        }
    }
}
