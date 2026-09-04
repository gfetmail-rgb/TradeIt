using System.Windows;
using System.Windows.Input;
using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _drawingEventRouterFixAttached;
        private Window? _drawingRouterWindow;

        private void AttachDrawingEventRouterFix()
        {
            if (_drawingEventRouterFixAttached) return;
            _drawingEventRouterFixAttached = true;

            // Preview tunneling at ChartTabView runs before WpfPlot's own
            // preview handler. This prevents the old bridge from interpreting
            // a horizontal Ray as a TrendLine and prevents ScottPlot from
            // consuming the cancellation click first.
            PreviewMouseLeftButtonDown += DrawingRouter_LeftDown;
            PreviewMouseMove += DrawingRouter_MouseMove;
            PreviewMouseRightButtonDown += DrawingRouter_RightDown;
            PreviewKeyDown += DrawingRouter_KeyDown;

            AddHandler(UIElement.PreviewMouseRightButtonDownEvent,
                new MouseButtonEventHandler(DrawingRouter_RightDown), true);
            AddHandler(Keyboard.PreviewKeyDownEvent,
                new System.Windows.Input.KeyEventHandler(DrawingRouter_KeyDown), true);

            _drawingRouterWindow = Window.GetWindow(this);
            if (_drawingRouterWindow != null)
            {
                _drawingRouterWindow.PreviewKeyDown += DrawingRouter_KeyDown;
                _drawingRouterWindow.AddHandler(
                    Keyboard.PreviewKeyDownEvent,
                    new System.Windows.Input.KeyEventHandler(DrawingRouter_KeyDown), true);
            }

            Unloaded += DrawingRouter_Unloaded;
        }

        private void DrawingRouter_Unloaded(object sender, RoutedEventArgs e)
        {
            if (!_drawingEventRouterFixAttached) return;
            _drawingEventRouterFixAttached = false;

            PreviewMouseLeftButtonDown -= DrawingRouter_LeftDown;
            PreviewMouseMove -= DrawingRouter_MouseMove;
            PreviewMouseRightButtonDown -= DrawingRouter_RightDown;
            PreviewKeyDown -= DrawingRouter_KeyDown;

            if (_drawingRouterWindow != null)
                _drawingRouterWindow.PreviewKeyDown -= DrawingRouter_KeyDown;
            _drawingRouterWindow = null;
            Unloaded -= DrawingRouter_Unloaded;
        }

        private bool IsRayOrAdvancedDrawing()
        {
            return _activeDrawingTool == TechnicalDrawingTool.Ray || IsAdvancedDrawingTool;
        }

        private void DrawingRouter_LeftDown(object sender, WpfMouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || _textDrawingActive)
                return;
            if (!IsRayOrAdvancedDrawing())
                return;

            AdvancedDrawing_MouseDown(sender, e);
            e.Handled = true;
        }

        private void DrawingRouter_MouseMove(object sender, WpfMouseEventArgs e)
        {
            if (_textDrawingActive || !IsRayOrAdvancedDrawing())
                return;

            AdvancedDrawing_MouseMove(sender, e);
            e.Handled = true;
        }

        private void DrawingRouter_RightDown(object sender, WpfMouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right)
                return;
            if (_activeDrawingTool == TechnicalDrawingTool.Select && !_textDrawingActive)
                return;

            _horizontalRayStart = null;
            RemoveHorizontalRayPreview();
            RemoveAdvancedPreview();
            _advancedDrawingP1 = null;
            _advancedDrawingP2 = null;
            CancelDrawingMode();
            e.Handled = true;
        }

        private void DrawingRouter_KeyDown(object sender, WpfKeyEventArgs e)
        {
            if (e.Key != Key.Escape && e.Key != Key.Cancel)
                return;
            if (_activeDrawingTool == TechnicalDrawingTool.Select && !_textDrawingActive)
                return;

            _horizontalRayStart = null;
            RemoveHorizontalRayPreview();
            RemoveAdvancedPreview();
            _advancedDrawingP1 = null;
            _advancedDrawingP2 = null;
            CancelDrawingMode();
            e.Handled = true;
        }
    }
}
