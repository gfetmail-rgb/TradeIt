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
                UIElement.PreviewMouseRightButtonDownEvent,
                new MouseButtonEventHandler(DrawingCancelAndArrowFix_RightMouseDown),
                true);
        }

        private void DrawingCancelAndArrowFix_RightMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right)
                return;

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
