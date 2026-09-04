using System.Windows;
using System.Windows.Input;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _arrowMeasurementBehaviorFixAttached;
        private static readonly bool _arrowMeasurementBehaviorFixRegistered = RegisterArrowMeasurementBehaviorFix();

        private static bool RegisterArrowMeasurementBehaviorFix()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(ArrowMeasurementBehaviorFix_Loaded));
            return true;
        }

        private static void ArrowMeasurementBehaviorFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.AttachArrowMeasurementBehaviorFix();
        }

        private void AttachArrowMeasurementBehaviorFix()
        {
            if (_arrowMeasurementBehaviorFixAttached)
                return;

            _arrowMeasurementBehaviorFixAttached = true;

            // The common Delete-All handler now owns confirmation and deletion.
            // Remove the old per-tool handlers so clicking No cannot delete arrows/measurements.
            DeleteAllDrawingsButton.Click -= ArrowDeleteAll_Click;
            DeleteAllDrawingsButton.Click -= MeasurementTool_DeleteAll;

            // Right-click while actively drawing an arrow cancels the unfinished arrow.
            Chart.PreviewMouseRightButtonDown += ArrowMeasurementBehaviorFix_RightMouseDown;

            // ESC cancels the active arrow drawing.
            AddHandler(
                Keyboard.PreviewKeyDownEvent,
                new System.Windows.Input.KeyEventHandler(ArrowMeasurementBehaviorFix_KeyDown),
                true);
        }

        private void ArrowMeasurementBehaviorFix_RightMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right)
                return;

            if (_arrowDrawingActive && (int)_activeDrawingTool == 10)
            {
                CancelArrowDrawing(true);
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | رسم پیکان لغو شد";
                e.Handled = true;
            }
        }

        private void ArrowMeasurementBehaviorFix_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
                return;

            if (_arrowDrawingActive && (int)_activeDrawingTool == 10)
            {
                CancelArrowDrawing(true);
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | رسم پیکان لغو شد";
                e.Handled = true;
            }
        }
    }
}
