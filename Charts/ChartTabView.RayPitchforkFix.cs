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

        private void InitializeDrawingCursorHandling()
        {
            if (_drawingCursorAttached) return;
            _drawingCursorAttached = true;

            DrawingSelectButton.Click += (_, _) => SetDrawingCursor(Cursors.Arrow);
            DrawingTrendLineButton.Click += (_, _) => SetDrawingCursor(Cursors.Cross);
            DrawingHorizontalLineButton.Click += (_, _) => SetDrawingCursor(Cursors.SizeNS);
            DrawingVerticalLineButton.Click += (_, _) => SetDrawingCursor(Cursors.SizeWE);
            DrawingRayButton.Click += (_, _) => SetDrawingCursor(Cursors.Cross);
            DrawingParallelChannelButton.Click += (_, _) => SetDrawingCursor(Cursors.Cross);
            DrawingRectangleButton.Click += (_, _) => SetDrawingCursor(Cursors.Cross);
            DrawingPitchforkButton.Click += (_, _) => SetDrawingCursor(Cursors.Cross);
            DrawingFibRetracementButton.Click += (_, _) => SetDrawingCursor(Cursors.Cross);
            DrawingFibExtensionButton.Click += (_, _) => SetDrawingCursor(Cursors.Cross);
            DrawingTextButton.Click += (_, _) => SetDrawingCursor(Cursors.IBeam);

            AddHandler(Keyboard.PreviewKeyDownEvent,
                new System.Windows.Input.KeyEventHandler(DrawingCursor_KeyDown), true);
            AddHandler(UIElement.PreviewMouseRightButtonDownEvent,
                new System.Windows.Input.MouseButtonEventHandler(DrawingCursor_RightMouseDown), true);
        }

        private void SetDrawingCursor(System.Windows.Input.Cursor cursor)
        {
            Cursor = cursor;
            Chart.Cursor = cursor;
        }

        private void RestoreDrawingCursor()
        {
            Cursor = Cursors.Arrow;
            Chart.Cursor = Cursors.Arrow;
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
            SetDrawingCursor(Cursors.Cross);
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
            SetDrawingCursor(Cursors.Cross);
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | چنگال اندروز: نقطه A را کلیک کنید";
        }
    }
}
