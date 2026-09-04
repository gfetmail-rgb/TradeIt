using System;
using System.Windows;
using System.Windows.Input;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _horizontalRayFixActive;
        private bool _horizontalRayFixAttached;
        private bool _drawingCursorAttached;
        private bool _advancedDirectInputAttached;

        private void InitializeDrawingCursorHandling()
        {
            if (_drawingCursorAttached) return;
            _drawingCursorAttached = true;

            DrawingSelectButton.Click += (_, _) => SetDrawingCursor(System.Windows.Input.Cursors.Arrow);
            DrawingTrendLineButton.Click += (_, _) => SetDrawingCursor(System.Windows.Input.Cursors.Cross);
            // Horizontal and vertical cursor shapes are intentionally swapped:
            // horizontal tool -> horizontal resize cursor, vertical tool -> vertical resize cursor.
            DrawingHorizontalLineButton.Click += (_, _) => SetDrawingCursor(System.Windows.Input.Cursors.SizeWE);
            DrawingVerticalLineButton.Click += (_, _) => SetDrawingCursor(System.Windows.Input.Cursors.SizeNS);
            DrawingRayButton.Click += (_, _) => SetDrawingCursor(System.Windows.Input.Cursors.Cross);
            DrawingParallelChannelButton.Click += (_, _) => SetDrawingCursor(System.Windows.Input.Cursors.Cross);
            DrawingRectangleButton.Click += (_, _) => SetDrawingCursor(System.Windows.Input.Cursors.Cross);
            DrawingPitchforkButton.Click += (_, _) => SetDrawingCursor(System.Windows.Input.Cursors.Cross);
            DrawingFibRetracementButton.Click += (_, _) => SetDrawingCursor(System.Windows.Input.Cursors.Cross);
            DrawingFibExtensionButton.Click += (_, _) => SetDrawingCursor(System.Windows.Input.Cursors.Cross);
            DrawingTextButton.Click += (_, _) => SetDrawingCursor(System.Windows.Input.Cursors.IBeam);

            // Advanced tools must receive chart clicks directly. The normal ChartTabView
            // preview handler and ScottPlot input path can otherwise consume the event
            // before UnifiedDrawing_PreProcessInput reaches the advanced tool state.
            if (!_advancedDirectInputAttached)
            {
                _advancedDirectInputAttached = true;
                Chart.PreviewMouseLeftButtonDown += AdvancedDirectMouseDown;
                Chart.PreviewMouseMove += AdvancedDirectMouseMove;
            }

            AddHandler(Keyboard.PreviewKeyDownEvent,
                new System.Windows.Input.KeyEventHandler(DrawingCursor_KeyDown), true);
            AddHandler(UIElement.PreviewMouseRightButtonDownEvent,
                new System.Windows.Input.MouseButtonEventHandler(DrawingCursor_RightMouseDown), true);
        }

        private void AdvancedDirectMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || _textDrawingActive || !IsAdvancedDrawingTool)
                return;

            AdvancedDrawing_MouseDown(sender, e);
        }

        private void AdvancedDirectMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_textDrawingActive || !IsAdvancedDrawingTool)
                return;

            AdvancedDrawing_MouseMove(sender, e);
        }

        private void SetDrawingCursor(System.Windows.Input.Cursor cursor)
        {
            Cursor = cursor;
            Chart.Cursor = cursor;
        }

        private void RestoreDrawingCursor()
        {
            Cursor = System.Windows.Input.Cursors.Arrow;
            Chart.Cursor = System.Windows.Input.Cursors.Arrow;
        }

        private void DrawingCursor_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Escape && e.Key != Key.Cancel) return;
            RestoreDrawingCursor();
        }

        private void DrawingCursor_RightMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right) return;
            RestoreDrawingCursor();
        }

        private void DrawingRayButton_Click_Horizontal(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            _textDrawingActive = false;

            _activeDrawingTool = TechnicalDrawingTool.Select;
            Chart.UserInputProcessor.IsEnabled = false;
            _horizontalRayFixActive = true;
            AttachHorizontalRayFixHandlers();
            UpdateTechnicalDrawingButtons();
            DrawingRayButton.Opacity = 1.0;
            DrawingParallelChannelButton.Opacity = 0.55;
            DrawingRectangleButton.Opacity = 0.55;
            DrawingPitchforkButton.Opacity = 0.55;
            DrawingFibRetracementButton.Opacity = 0.55;
            DrawingFibExtensionButton.Opacity = 0.55;
            SetDrawingCursor(System.Windows.Input.Cursors.Cross);
            Chart.Focusable = true;
            Chart.Focus();
            Focus();
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | نیم‌خط افقی: محل شروع را کلیک کنید";
            Chart.Refresh();
        }

        private void AttachHorizontalRayFixHandlers()
        {
            if (_horizontalRayFixAttached) return;
            _horizontalRayFixAttached = true;
            Chart.PreviewMouseLeftButtonDown += HorizontalRayFix_MouseDown;
            AddHandler(Keyboard.PreviewKeyDownEvent,
                new System.Windows.Input.KeyEventHandler(HorizontalRayFix_KeyDown), true);
            AddHandler(UIElement.PreviewMouseRightButtonDownEvent,
                new System.Windows.Input.MouseButtonEventHandler(HorizontalRayFix_RightMouseDown), true);
        }

        private void DetachHorizontalRayFixHandlers()
        {
            if (!_horizontalRayFixAttached) return;
            Chart.PreviewMouseLeftButtonDown -= HorizontalRayFix_MouseDown;
            RemoveHandler(Keyboard.PreviewKeyDownEvent,
                new System.Windows.Input.KeyEventHandler(HorizontalRayFix_KeyDown));
            RemoveHandler(UIElement.PreviewMouseRightButtonDownEvent,
                new System.Windows.Input.MouseButtonEventHandler(HorizontalRayFix_RightMouseDown));
            _horizontalRayFixAttached = false;
        }

        private void HorizontalRayFix_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_horizontalRayFixActive || e.ChangedButton != MouseButton.Left || _textDrawingActive) return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates point)) return;

            int index = FindNearestDrawingBarIndex(point.X);
            if (index < 0) return;
            point = new ScottPlot.Coordinates(GetDrawingX(index), point.Y);

            var ray = new RayDrawing
            {
                X1 = point.X,
                Y1 = point.Y,
                X2 = point.X + 1.0,
                Y2 = point.Y
            };
            _rays.Add(ray);
            AddRayToChart(ray);

            _horizontalRayFixActive = false;
            DetachHorizontalRayFixHandlers();
            Chart.UserInputProcessor.IsEnabled = true;
            RestoreDrawingCursor();
            DrawingRayButton.Opacity = 0.55;
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | نیم‌خط افقی رسم شد";
            Chart.Refresh();
            e.Handled = true;
        }

        private void HorizontalRayFix_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!_horizontalRayFixActive || (e.Key != Key.Escape && e.Key != Key.Cancel)) return;
            CancelHorizontalRayFix();
            e.Handled = true;
        }

        private void HorizontalRayFix_RightMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_horizontalRayFixActive || e.ChangedButton != MouseButton.Right) return;
            CancelHorizontalRayFix();
            e.Handled = true;
        }

        private void CancelHorizontalRayFix()
        {
            _horizontalRayFixActive = false;
            DetachHorizontalRayFixHandlers();
            Chart.UserInputProcessor.IsEnabled = true;
            RestoreDrawingCursor();
            UpdateTechnicalDrawingButtons();
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | رسم ابزار لغو شد";
            Chart.Refresh();
        }

        private void DrawingPitchforkButton_Click_Direct(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            _textDrawingActive = false;
            _horizontalRayFixActive = false;
            DetachHorizontalRayFixHandlers();
            SetAdvancedDrawingTool(AdvancedToolPitchfork);
            SetDrawingCursor(System.Windows.Input.Cursors.Cross);
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | چنگال اندروز: نقطه A را کلیک کنید";
        }
    }
}
