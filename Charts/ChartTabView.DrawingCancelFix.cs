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

        private void AttachDrawingCancelFix()
        {
            if (_drawingCancelFixAttached) return;
            _drawingCancelFixAttached = true;

            PreviewMouseRightButtonDown += DrawingCancelFix_RightMouseDown;
            PreviewKeyDown += DrawingCancelFix_KeyDown;
            Chart.PreviewMouseMove += DrawingCancelFix_MouseMove;
            Unloaded += DrawingCancelFix_Unloaded;
        }

        private void DrawingCancelFix_Unloaded(object sender, RoutedEventArgs e)
        {
            if (!_drawingCancelFixAttached) return;
            _drawingCancelFixAttached = false;

            PreviewMouseRightButtonDown -= DrawingCancelFix_RightMouseDown;
            PreviewKeyDown -= DrawingCancelFix_KeyDown;
            Chart.PreviewMouseMove -= DrawingCancelFix_MouseMove;
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
