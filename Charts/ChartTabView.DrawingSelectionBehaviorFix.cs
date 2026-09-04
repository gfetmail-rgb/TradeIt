using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _drawingSelectionBehaviorFixAttached;
        private bool _selectionMouseMoved;

        private static readonly bool _drawingSelectionBehaviorFixRegistered = RegisterDrawingSelectionBehaviorFix();

        private static bool RegisterDrawingSelectionBehaviorFix()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(DrawingSelectionBehaviorFix_Loaded));
            return true;
        }

        private static void DrawingSelectionBehaviorFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart) chart.AttachDrawingSelectionBehaviorFix();
        }

        private void AttachDrawingSelectionBehaviorFix()
        {
            if (_drawingSelectionBehaviorFixAttached) return;
            _drawingSelectionBehaviorFixAttached = true;

            Chart.PreviewMouseLeftButtonDown -= DrawingSelection_MouseDown;
            Chart.PreviewMouseMove -= DrawingSelection_MouseMove;
            Chart.PreviewMouseLeftButtonUp -= DrawingSelection_MouseUp;

            Chart.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent,
                new System.Windows.Input.MouseButtonEventHandler(DrawingSelectionBehaviorFix_MouseDown), true);
            Chart.AddHandler(Mouse.PreviewMouseMoveEvent,
                new System.Windows.Input.MouseEventHandler(DrawingSelectionBehaviorFix_MouseMove), true);
            Chart.AddHandler(UIElement.PreviewMouseLeftButtonUpEvent,
                new System.Windows.Input.MouseButtonEventHandler(DrawingSelectionBehaviorFix_MouseUp), true);
            Chart.AddHandler(UIElement.PreviewMouseRightButtonDownEvent,
                new System.Windows.Input.MouseButtonEventHandler(DrawingSelectionBehaviorFix_RightMouseDown), true);
        }

        private void DrawingSelectionBehaviorFix_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || _activeDrawingTool != TechnicalDrawingTool.Select)
                return;
            if (!TryGetRawChartPoint(e, out ScottPlot.Coordinates chartPoint)) return;

            // Text is a drawing too, but has its own model and handles.
            if (_textSelection != null)
            {
                if (TryGetTextSelectionHandle(chartPoint, out _))
                {
                    BeginTextSelectionDrag(chartPoint);
                    e.Handled = true;
                    return;
                }

                if (IsPointOnSelectedText(chartPoint))
                {
                    BeginTextSelectionDrag(chartPoint);
                    e.Handled = true;
                    return;
                }
            }

            if (TrySelectTextDrawing(chartPoint))
            {
                _selectionDragging = false;
                _activeDrawingHandle = null;
                Chart.UserInputProcessor.IsEnabled = false;
                e.Handled = true;
                Chart.Refresh();
                return;
            }

            // A visible anchor always wins over body hit-testing.
            if (_selectedDrawing != null && TryGetHandleAtPoint(chartPoint, out DrawingHandleKind handleKind))
            {
                _activeDrawingHandle = handleKind;
                _selectionDragStart = chartPoint;
                _selectionMouseMoved = false;
                _selectionDragging = true;
                Chart.CaptureMouse();
                Chart.UserInputProcessor.IsEnabled = false;
                e.Handled = true;
                return;
            }

            // Use the stricter hit-test implementation. In particular, Fibonacci
            // levels are tested only inside their actual X range.
            if (TrySelectDrawingAccurate(chartPoint))
            {
                _selectionDragging = false;
                _selectionMouseMoved = false;
                Chart.ReleaseMouseCapture();
                Chart.UserInputProcessor.IsEnabled = false;
                e.Handled = true;
                Chart.Refresh();
            }
            else
            {
                ClearTextSelection();
                ClearDrawingSelection();
            }
        }

        private void DrawingSelectionBehaviorFix_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_textSelectionDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                if (TryGetRawChartPoint(e, out ScottPlot.Coordinates point))
                {
                    if (MoveSelectedText(point))
                    {
                        e.Handled = true;
                        Chart.Refresh();
                    }
                }
                return;
            }

            if (!_selectionDragging || _selectedDrawing == null || _activeDrawingHandle == null || e.LeftButton != MouseButtonState.Pressed)
                return;
            if (!TryGetRawChartPoint(e, out ScottPlot.Coordinates point2)) return;

            if (Math.Abs(point2.X - _selectionDragStart.X) < 1e-15 && Math.Abs(point2.Y - _selectionDragStart.Y) < 1e-15)
                return;

            _selectionMouseMoved = true;
            if (MoveSelectedHandle(_activeDrawingHandle.Value, point2))
            {
                _selectionDragStart = point2;
                e.Handled = true;
                Chart.Refresh();
            }
        }

        private void DrawingSelectionBehaviorFix_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            if (_textSelectionDragging)
            {
                EndTextSelectionDrag();
                e.Handled = true;
                return;
            }

            if (!_selectionDragging) return;
            _selectionDragging = false;
            _selectionMouseMoved = false;
            _activeDrawingHandle = null;
            Chart.ReleaseMouseCapture();
            Chart.UserInputProcessor.IsEnabled = false;
            e.Handled = true;
            Chart.Refresh();
        }

        private void DrawingSelectionBehaviorFix_RightMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right || _activeDrawingTool != TechnicalDrawingTool.Select)
                return;
            if (!TryGetRawChartPoint(e, out ScottPlot.Coordinates point)) return;

            if (_textSelection != null && IsPointOnSelectedText(point))
            {
                ShowTextSelectionContextMenu();
                e.Handled = true;
                return;
            }

            if (_selectedDrawing == null || !IsPointOnSelectedDrawing(point)) return;

            var menu = new ContextMenu();
            var deleteItem = new MenuItem { Header = "حذف" };
            deleteItem.Click += (_, _) => DeleteSelectedDrawing();
            menu.Items.Add(deleteItem);
            menu.IsOpen = true;
            e.Handled = true;
        }
    }
}
