using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _drawingButtonVisualFixAttached;
        private static readonly bool _drawingButtonVisualFixRegistered = RegisterDrawingButtonVisualFix();

        private static bool RegisterDrawingButtonVisualFix()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(DrawingButtonVisualFix_Loaded));
            return true;
        }

        private static void DrawingButtonVisualFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.AttachDrawingButtonVisualFix();
        }

        private void AttachDrawingButtonVisualFix()
        {
            if (_drawingButtonVisualFixAttached)
                return;

            _drawingButtonVisualFixAttached = true;

            DrawingSelectButton.Click += DrawingButtonVisualFix_Click;
            DrawingTrendLineButton.Click += DrawingButtonVisualFix_Click;
            DrawingHorizontalLineButton.Click += DrawingButtonVisualFix_Click;
            DrawingVerticalLineButton.Click += DrawingButtonVisualFix_Click;
            DrawingRayButton.Click += DrawingButtonVisualFix_Click;
            DrawingParallelChannelButton.Click += DrawingButtonVisualFix_Click;
            DrawingRectangleButton.Click += DrawingButtonVisualFix_Click;
            DrawingPitchforkButton.Click += DrawingButtonVisualFix_Click;
            DrawingFibRetracementButton.Click += DrawingButtonVisualFix_Click;
            DrawingFibExtensionButton.Click += DrawingButtonVisualFix_Click;
            DrawingTextButton.Click += DrawingButtonVisualFix_Click;

            AddHandler(Keyboard.PreviewKeyDownEvent,
                new System.Windows.Input.KeyEventHandler(DrawingButtonVisualFix_KeyDown), true);
            AddHandler(UIElement.PreviewMouseRightButtonDownEvent,
                new System.Windows.Input.MouseButtonEventHandler(DrawingButtonVisualFix_RightMouseDown), true);

            Dispatcher.BeginInvoke(new Action(SetAllDrawingButtonVisuals), DispatcherPriority.ContextIdle);
        }

        private void DrawingButtonVisualFix_Click(object sender, RoutedEventArgs e)
        {
            // Run after the tool's own Click handler has changed the active state.
            Dispatcher.BeginInvoke(new Action(ApplyDrawingButtonVisualFix), DispatcherPriority.ContextIdle);
        }

        private void ApplyDrawingButtonVisualFix()
        {
            SetAllDrawingButtonVisuals();

            // The horizontal-ray compatibility path intentionally keeps
            // _activeDrawingTool == Select, so it needs an explicit active state.
            if (_horizontalRayFixActive)
            {
                RestoreAllDrawingButtonVisuals();
                SetDrawingButtonVisual(DrawingRayButton, true);
            }
        }

        private void DrawingButtonVisualFix_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Escape && e.Key != Key.Cancel)
                return;

            Dispatcher.BeginInvoke(new Action(ApplyDrawingButtonVisualFix), DispatcherPriority.ContextIdle);
        }

        private void DrawingButtonVisualFix_RightMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right)
                return;

            // If a drawing tool is being cancelled before its first point, restore
            // the original toolbar appearance after the cancellation handler runs.
            if (_activeDrawingTool != TechnicalDrawingTool.Select ||
                _textDrawingActive || _horizontalRayFixActive)
            {
                Dispatcher.BeginInvoke(new Action(ApplyDrawingButtonVisualFix), DispatcherPriority.ContextIdle);
            }
        }
    }
}
