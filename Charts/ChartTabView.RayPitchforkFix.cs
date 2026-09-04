using System;
using System.Windows;
using System.Windows.Input;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _horizontalRayFixActive;
        private bool _horizontalRayFixAttached;

        private void DrawingRayButton_Click_Horizontal(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            _textDrawingActive = false;

            // Do not activate TechnicalDrawingTool.Ray: its legacy two-click handler
            // would intercept the same mouse event. The replacement below is one-click.
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

            // X2 is one unit to the right and Y2 is identical, so AddRayToChart()
            // extends this stored ray horizontally to the visible right edge.
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
            Chart.UserInputProcessor.IsEnabled = true;
            DrawingRayButton.Opacity = 0.55;
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | نیم‌خط افقی رسم شد";
            Chart.Refresh();
            e.Handled = true;
        }

        private void HorizontalRayFix_KeyDown(object sender, KeyEventArgs e)
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
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | چنگال اندروز: نقطه A را کلیک کنید";
        }
    }
}
