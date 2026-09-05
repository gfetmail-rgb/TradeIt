using System;
using System.Windows;
using System.Windows.Input;
using TradeIt.Charts;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _arrowMeasurementBehaviorFixAttached;
        private static readonly bool _arrowMeasurementBehaviorFixRegistered = RegisterArrowMeasurementBehaviorFix();

        private static bool RegisterArrowMeasurementBehaviorFix()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent, new RoutedEventHandler(ArrowMeasurementBehaviorFix_Loaded));
            return true;
        }

        private static void ArrowMeasurementBehaviorFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart) chart.AttachArrowMeasurementBehaviorFix();
        }

        private void AttachArrowMeasurementBehaviorFix()
        {
            if (_arrowMeasurementBehaviorFixAttached) return;
            _arrowMeasurementBehaviorFixAttached = true;
            DeleteAllDrawingsButton.Click -= ArrowDeleteAll_Click;
            Chart.PreviewMouseRightButtonDown += ArrowMeasurementBehaviorFix_RightMouseDown;
            AddHandler(Keyboard.PreviewKeyDownEvent, new System.Windows.Input.KeyEventHandler(ArrowMeasurementBehaviorFix_KeyDown), true);
        }

        private void ArrowMeasurementBehaviorFix_RightMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right) return;
            if (_arrowDrawingActive && (int)_activeDrawingTool == 10)
            {
                CancelArrowDrawing(true);
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | رسم پیکان لغو شد";
                e.Handled = true;
            }
        }

        private void ArrowMeasurementBehaviorFix_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Escape) return;
            if (_arrowDrawingActive && (int)_activeDrawingTool == 10)
            {
                CancelArrowDrawing(true);
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | رسم پیکان لغو شد";
                e.Handled = true;
            }
        }

        private bool _drawingCursorAttached;
        private bool _advancedDirectInputAttached;

        private void InitializeDrawingCursorHandling()
        {
            if (_drawingCursorAttached) return;
            _drawingCursorAttached = true;
            DrawingSelectButton.Click += (_, _) => SetDrawingCursor(System.Windows.Input.Cursors.Arrow);
            DrawingTrendLineButton.Click += (_, _) => SetDrawingCursor(System.Windows.Input.Cursors.Cross);
            DrawingHorizontalLineButton.Click += (_, _) => SetDrawingCursor(System.Windows.Input.Cursors.SizeWE);
            DrawingVerticalLineButton.Click += (_, _) => SetDrawingCursor(System.Windows.Input.Cursors.SizeNS);
            DrawingRayButton.Click += (_, _) => SetDrawingCursor(System.Windows.Input.Cursors.Cross);
            DrawingParallelChannelButton.Click += (_, _) => SetDrawingCursor(System.Windows.Input.Cursors.Cross);
            DrawingRectangleButton.Click += (_, _) => SetDrawingCursor(System.Windows.Input.Cursors.Cross);
            DrawingPitchforkButton.Click += (_, _) => SetDrawingCursor(System.Windows.Input.Cursors.Cross);
            DrawingFibRetracementButton.Click += (_, _) => SetDrawingCursor(System.Windows.Input.Cursors.Cross);
            DrawingFibExtensionButton.Click += (_, _) => SetDrawingCursor(System.Windows.Input.Cursors.Cross);
            DrawingTextButton.Click += (_, _) => SetDrawingCursor(System.Windows.Input.Cursors.IBeam);
            if (!_advancedDirectInputAttached)
            {
                _advancedDirectInputAttached = true;
                Chart.PreviewMouseLeftButtonDown += AdvancedDirectMouseDown;
                Chart.PreviewMouseMove += AdvancedDirectMouseMove;
            }
            AddHandler(Keyboard.PreviewKeyDownEvent, new System.Windows.Input.KeyEventHandler(DrawingCursor_KeyDown), true);
            AddHandler(UIElement.PreviewMouseRightButtonDownEvent, new System.Windows.Input.MouseButtonEventHandler(DrawingCursor_RightMouseDown), true);
        }

        private void AdvancedDirectMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || _textDrawingActive || !IsAdvancedDrawingTool) return;
            AdvancedDrawing_MouseDown(sender, e);
        }

        private void AdvancedDirectMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_textDrawingActive || !IsAdvancedDrawingTool) return;
            AdvancedDrawing_MouseMove(sender, e);
        }

        private void SetDrawingCursor(System.Windows.Input.Cursor cursor) { Cursor = cursor; Chart.Cursor = cursor; }
        private void RestoreDrawingCursor() { Cursor = System.Windows.Input.Cursors.Arrow; Chart.Cursor = System.Windows.Input.Cursors.Arrow; }
        private void DrawingCursor_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) { if (e.Key != Key.Escape && e.Key != Key.Cancel) return; RestoreDrawingCursor(); }
        private void DrawingCursor_RightMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) { if (e.ChangedButton != MouseButton.Right) return; RestoreDrawingCursor(); }

        private void DrawingPitchforkButton_Click_Direct(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            _textDrawingActive = false;
            SetAdvancedDrawingTool(AdvancedToolPitchfork);
            SetDrawingCursor(System.Windows.Input.Cursors.Cross);
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | چنگال اندروز: نقطه A را کلیک کنید";
        }
    }
}