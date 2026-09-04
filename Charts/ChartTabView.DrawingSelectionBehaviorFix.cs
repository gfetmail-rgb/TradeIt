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
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(DrawingSelectionBehaviorFix_Loaded));
            return true;
        }

        private static void DrawingSelectionBehaviorFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.AttachDrawingSelectionBehaviorFix();
        }

        private void AttachDrawingSelectionBehaviorFix()
        {
            if (_drawingSelectionBehaviorFixAttached) return;
            _drawingSelectionBehaviorFixAttached = true;

            // Replace the original selection handlers so a single click only selects.
            // A second mouse-down on the selected object starts the drag operation.
            Chart.PreviewMouseLeftButtonDown -= DrawingSelection_MouseDown;
            Chart.PreviewMouseMove -= DrawingSelection_MouseMove;
            Chart.PreviewMouseLeftButtonUp -= DrawingSelection_MouseUp;

            Chart.PreviewMouseLeftButtonDown += DrawingSelectionBehaviorFix_MouseDown;
            Chart.PreviewMouseMove += DrawingSelectionBehaviorFix_MouseMove;
            Chart.PreviewMouseLeftButtonUp += DrawingSelectionBehaviorFix_MouseUp;
            Chart.PreviewMouseRightButtonDown += DrawingSelectionBehaviorFix_RightMouseDown;
        }

        private void DrawingSelectionBehaviorFix_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || _textDrawingActive || _activeDrawingTool != TechnicalDrawingTool.Select)
                return;
            if (!TryGetRawChartPoint(e, out ScottPlot.Coordinates point)) return;

            bool clickedSelected = _selectedDrawing != null && IsPointOnSelectedDrawing(point);

            if (clickedSelected)
            {
                _selectionDragStart = point;
                _selectionDragging = true;
                _selectionMouseMoved = false;
                Chart.CaptureMouse();
                Chart.UserInputProcessor.IsEnabled = false;
                e.Handled = true;
                return;
            }

            if (TrySelectDrawing(point))
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
                ClearDrawingSelection();
                Chart.UserInputProcessor.IsEnabled = true;
            }
        }

        private void DrawingSelectionBehaviorFix_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_selectionDragging || _selectedDrawing == null || e.LeftButton != MouseButtonState.Pressed)
                return;
            if (!TryGetRawChartPoint(e, out ScottPlot.Coordinates point)) return;

            double dx = point.X - _selectionDragStart.X;
            double dy = point.Y - _selectionDragStart.Y;
            if (Math.Abs(dx) < 1e-15 && Math.Abs(dy) < 1e-15) return;

            _selectionMouseMoved = true;
            MoveSelectedDrawing(dx, dy);
            _selectionDragStart = point;
            e.Handled = true;
            Chart.Refresh();
        }

        private void DrawingSelectionBehaviorFix_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || !_selectionDragging)
                return;

            bool wasDrag = _selectionMouseMoved;
            _selectionDragging = false;
            _selectionMouseMoved = false;
            Chart.ReleaseMouseCapture();
            Chart.UserInputProcessor.IsEnabled = false;

            // Second click without movement = deselect.
            if (!wasDrag)
            {
                ClearDrawingSelection();
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | انتخاب ابزار لغو شد";
            }

            e.Handled = true;
            Chart.Refresh();
        }

        private void DrawingSelectionBehaviorFix_RightMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right || _activeDrawingTool != TechnicalDrawingTool.Select || _textDrawingActive)
                return;
            if (!TryGetRawChartPoint(e, out ScottPlot.Coordinates point)) return;
            if (_selectedDrawing == null || !IsPointOnSelectedDrawing(point)) return;

            var menu = new ContextMenu();
            var deleteItem = new MenuItem { Header = "حذف" };
            deleteItem.Click += (_, _) => DeleteSelectedDrawing();
            menu.Items.Add(deleteItem);
            menu.IsOpen = true;
            e.Handled = true;
        }

        private bool IsPointOnSelectedDrawing(ScottPlot.Coordinates point)
        {
            if (_selectedDrawing == null) return false;
            double tolerance = GetDrawingHitTolerance();

            switch (_selectedDrawingKind)
            {
                case DrawingSelectionKind.Fibonacci:
                    return DistanceToFibonacci(point, (FibonacciDrawing)_selectedDrawing) <= tolerance;
                case DrawingSelectionKind.Pitchfork:
                    return DistanceToPitchfork(point, (PitchforkDrawing)_selectedDrawing) <= tolerance;
                case DrawingSelectionKind.ParallelChannel:
                    {
                        var d = (ParallelChannelDrawing)_selectedDrawing;
                        return DistancePointToSegment(point, d.A, d.B) <= tolerance ||
                               DistancePointToSegment(point, d.C,
                                   new ScottPlot.Coordinates(d.C.X + d.B.X - d.A.X, d.C.Y + d.B.Y - d.A.Y)) <= tolerance;
                    }
                case DrawingSelectionKind.Rectangle:
                    {
                        var d = (RectangleDrawing)_selectedDrawing;
                        double left = Math.Min(d.A.X, d.B.X), right = Math.Max(d.A.X, d.B.X);
                        double bottom = Math.Min(d.A.Y, d.B.Y), top = Math.Max(d.A.Y, d.B.Y);
                        return DistancePointToSegment(point, new ScottPlot.Coordinates(left, bottom), new ScottPlot.Coordinates(right, bottom)) <= tolerance ||
                               DistancePointToSegment(point, new ScottPlot.Coordinates(right, bottom), new ScottPlot.Coordinates(right, top)) <= tolerance ||
                               DistancePointToSegment(point, new ScottPlot.Coordinates(right, top), new ScottPlot.Coordinates(left, top)) <= tolerance ||
                               DistancePointToSegment(point, new ScottPlot.Coordinates(left, top), new ScottPlot.Coordinates(left, bottom)) <= tolerance;
                    }
                case DrawingSelectionKind.Ray:
                    {
                        var d = (RayDrawing)_selectedDrawing;
                        return DistanceToRay(point, d.X1, d.Y1, d.X2, d.Y2) <= tolerance;
                    }
                case DrawingSelectionKind.TrendLine:
                    {
                        var d = (TrendLineDrawing)_selectedDrawing;
                        return DistancePointToSegment(point,
                            new ScottPlot.Coordinates(d.X1, d.Y1),
                            new ScottPlot.Coordinates(d.X2, d.Y2)) <= tolerance;
                    }
                case DrawingSelectionKind.HorizontalLine:
                    return Math.Abs(point.Y - ((HorizontalLineDrawing)_selectedDrawing).Y) <= tolerance;
                case DrawingSelectionKind.VerticalLine:
                    return Math.Abs(point.X - ((VerticalLineDrawing)_selectedDrawing).X) <= tolerance;
                default:
                    return false;
            }
        }
    }
}
