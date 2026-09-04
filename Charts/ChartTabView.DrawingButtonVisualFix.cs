using System;
using System.Windows;
using System.Windows.Controls;
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
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent,
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

            AttachDrawingButton(DrawingSelectButton);
            AttachDrawingButton(DrawingTrendLineButton);
            AttachDrawingButton(DrawingHorizontalLineButton);
            AttachDrawingButton(DrawingVerticalLineButton);
            AttachDrawingButton(DrawingRayButton);
            AttachDrawingButton(DrawingParallelChannelButton);
            AttachDrawingButton(DrawingRectangleButton);
            AttachDrawingButton(DrawingPitchforkButton);
            AttachDrawingButton(DrawingFibRetracementButton);
            AttachDrawingButton(DrawingFibExtensionButton);
            AttachDrawingButton(DrawingTextButton);

            AddHandler(Keyboard.PreviewKeyDownEvent,
                new System.Windows.Input.KeyEventHandler(DrawingButtonVisualFix_KeyDown), true);
            AddHandler(UIElement.PreviewMouseRightButtonDownEvent,
                new System.Windows.Input.MouseButtonEventHandler(DrawingButtonVisualFix_RightMouseDown), true);

            Dispatcher.BeginInvoke(new Action(SetAllDrawingButtonVisuals), DispatcherPriority.ContextIdle);
        }

        private void AttachDrawingButton(System.Windows.Controls.Button button)
        {
            button.AddHandler(
                System.Windows.Controls.Button.ClickEvent,
                new RoutedEventHandler(DrawingButtonVisualFix_Click),
                true);
        }

        private void DrawingButtonVisualFix_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(ApplyDrawingButtonVisualFix), DispatcherPriority.ContextIdle);
        }

        private void ApplyDrawingButtonVisualFix()
        {
            SetAllDrawingButtonVisuals();
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

            if (_activeDrawingTool != TechnicalDrawingTool.Select || _textDrawingActive)
            {
                Dispatcher.BeginInvoke(new Action(ApplyDrawingButtonVisualFix), DispatcherPriority.ContextIdle);
            }
        }
    }
}
