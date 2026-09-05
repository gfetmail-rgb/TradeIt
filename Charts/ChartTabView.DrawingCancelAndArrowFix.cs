using System.Windows;
using System.Windows.Input;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _drawingCancelAndArrowFixAttached;
        private static readonly bool _drawingCancelAndArrowFixRegistered = RegisterDrawingCancelAndArrowFix();

        private static bool RegisterDrawingCancelAndArrowFix()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(DrawingCancelAndArrowFix_Loaded));
            return true;
        }

        private static void DrawingCancelAndArrowFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.AttachDrawingCancelAndArrowFix();
        }

        private void AttachDrawingCancelAndArrowFix()
        {
            if (_drawingCancelAndArrowFixAttached) return;
            _drawingCancelAndArrowFixAttached = true;

            Chart.AddHandler(
                UIElement.PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler(DrawingCancelAndArrowFix_LeftMouseDown),
                true);
            Chart.AddHandler(
                UIElement.PreviewMouseMoveEvent,
                new MouseEventHandler(DrawingCancelAndArrowFix_MouseMove),
                true);
            Chart.AddHandler(
                UIElement.PreviewMouseRightButtonDownEvent,
                new MouseButtonEventHandler(DrawingCancelAndArrowFix_RightMouseDown),
                true);
        }

        private void DrawingCancelAndArrowFix_LeftMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || _textDrawingActive)
                return;

            if ((int)_activeDrawingTool == 10 && !e.Handled)
            {
                ArrowDrawing_MouseDown(Chart, e);
                e.Handled = true;
            }
        }

        private void DrawingCancelAndArrowFix_MouseMove(object sender, MouseEventArgs e)
        {
            if (_textDrawingActive)
                return;

            if ((int)_activeDrawingTool == 10 && !e.Handled)
            {
                ArrowDrawing_MouseMove(Chart, e);
                e.Handled = true;
            }
        }

        private void DrawingCancelAndArrowFix_RightMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right)
                return;

            // TechnicalDrawing_RightMouseDown runs on the parent first and changes
            // the active tool to Select. Clean every uncommitted preview here as well.
            RemoveUnifiedFibPreview();
            _unifiedFibP1 = null;
            _unifiedFibP2 = null;
            RemoveAdvancedPreview();
            _advancedDrawingP1 = null;
            _advancedDrawingP2 = null;
            RemoveHorizontalRayPreview();
            _horizontalRayStart = null;
            RemoveTrendLinePreview();
            _trendLineStart = null;
            RemoveArrowPreview();
            _pendingArrowStart = null;
            _arrowDrawingActive = false;
            SetArrowButtonVisual(false);
            _textDrawingActive = false;
            Chart.ReleaseMouseCapture();
            Chart.UserInputProcessor.IsEnabled = true;
            UpdateTechnicalDrawingButtons();
            UpdateFibonacciButtonVisualState();
            Chart.Refresh();
        }
    }
}
