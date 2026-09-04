using System.Windows;
using System.Windows.Input;
using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _drawingCancelFixAttached;
        private Window? _drawingHostWindow;

        private void AttachDrawingCancelFix()
        {
            if (_drawingCancelFixAttached) return;
            _drawingCancelFixAttached = true;

            // Handle cancellation directly on the chart as well as on the
            // UserControl. ScottPlot can consume mouse/keyboard input at the
            // chart level, so relying only on the parent routed event is not
            // sufficient.
            Chart.PreviewMouseRightButtonDown += DrawingCancelFix_RightMouseDown;
            PreviewMouseRightButtonDown += DrawingCancelFix_RightMouseDown;
            Chart.PreviewKeyDown += DrawingCancelFix_KeyDown;
            PreviewKeyDown += DrawingCancelFix_KeyDown;
            Chart.PreviewMouseMove += DrawingCancelFix_MouseMove;

            _drawingHostWindow = Window.GetWindow(this);
            if (_drawingHostWindow != null)
                _drawingHostWindow.PreviewKeyDown += DrawingCancelFix_KeyDown;

            Unloaded += DrawingCancelFix_Unloaded;
        }

        private void DrawingCancelFix_Unloaded(object sender, RoutedEventArgs e)
        {
            if (!_drawingCancelFixAttached) return;
            _drawingCancelFixAttached = false;

            Chart.PreviewMouseRightButtonDown -= DrawingCancelFix_RightMouseDown;
            PreviewMouseRightButtonDown -= DrawingCancelFix_RightMouseDown;
            Chart.PreviewKeyDown -= DrawingCancelFix_KeyDown;
            PreviewKeyDown -= DrawingCancelFix_KeyDown;
            Chart.PreviewMouseMove -= DrawingCancelFix_MouseMove;

            if (_drawingHostWindow != null)
                _drawingHostWindow.PreviewKeyDown -= DrawingCancelFix_KeyDown;
            _drawingHostWindow = null;
            Unloaded -= DrawingCancelFix_Unloaded;
        }

        private void DrawingCancelFix_RightMouseDown(object sender, WpfMouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right) return;
            if (_activeDrawingTool == TechnicalDrawingTool.Select && !_textDrawingActive) return;

            CancelDrawingMode();
            e.Handled = true;
        }

        private void DrawingCancelFix_KeyDown(object sender, WpfKeyEventArgs e)
        {
            if (e.Key != Key.Escape) return;
            if (_activeDrawingTool == TechnicalDrawingTool.Select && !_textDrawingActive) return;

            CancelDrawingMode();
            e.Handled = true;
        }

        private void DrawingCancelFix_MouseMove(object sender, WpfMouseEventArgs e)
        {
            if (_textDrawingActive) return;
            if (_activeDrawingTool != TechnicalDrawingTool.Ray) return;

            TechnicalDrawing_MouseMove(sender, e);
        }
    }
}
