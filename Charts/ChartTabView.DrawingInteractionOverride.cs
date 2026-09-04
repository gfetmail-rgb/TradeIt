using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _drawingInteractionOverrideAttached;
        private bool _drawingSelectionMoved;
        private object? _drawingSelectionAtMouseDown;

        private static readonly bool _drawingInteractionOverrideRegistered = RegisterDrawingInteractionOverride();

        private static bool RegisterDrawingInteractionOverride()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(DrawingInteractionOverride_Loaded));
            return true;
        }

        private static void DrawingInteractionOverride_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.AttachDrawingInteractionOverride();
        }

        private void AttachDrawingInteractionOverride()
        {
            if (_drawingInteractionOverrideAttached) return;
            _drawingInteractionOverrideAttached = true;

            InputManager.Current.PreProcessInput += DrawingInteractionOverride_PreProcessInput;
            SetAllDrawingButtonVisuals();
        }

        private void DrawingInteractionOverride_PreProcessInput(object sender, PreProcessInputEventArgs e)
        {
            if (e.StagingItem.Input is MouseButtonEventArgs button)
            {
                if (!SourceBelongsToThisChart(button.OriginalSource as DependencyObject)) return;

                if (button.ChangedButton == MouseButton.Left &&
                    _activeDrawingTool == TechnicalDrawingTool.Select &&
                    !_textDrawingActive)
                {
                    if (!TryGetRawChartPoint(button, out ScottPlot.Coordinates point)) return;

                    object? oldSelection = _selectedDrawing;
                    bool hadSelection = oldSelection != null;
                    bool hit = TrySelectDrawing(point);

                    if (!hit)
                    {
                        ClearDrawingSelection();
                        return;
                    }

                    if (hadSelection && ReferenceEquals(oldSelection, _selectedDrawing))
                    {
                        _drawingSelectionAtMouseDown = _selectedDrawing;
                        _drawingSelectionMoved = false;
                    }
                    else
                    {
                        _drawingSelectionAtMouseDown = null;
                        _drawingSelectionMoved = false;
                    }

                    _selectionDragStart = point;
                    _selectionDragging = true;
                    Chart.CaptureMouse();
                    Chart.UserInputProcessor.IsEnabled = false;
                    button.Handled = true;
                    return;
                }

                if (button.ChangedButton == MouseButton.Right &&
                    _activeDrawingTool == TechnicalDrawingTool.Select &&
                    !_textDrawingActive &&
                    _selectedDrawing != null)
                {
                    if (!TryGetRawChartPoint(button, out ScottPlot.Coordinates point)) return;
                    if (!IsPointOnSelectedDrawing(point)) return;

                    ShowSelectedDrawingSettings();
                    button.Handled = true;
                    return;
                }

                if (button.ChangedButton == MouseButton.Left &&
                    _activeDrawingTool != TechnicalDrawingTool.Select &&
                    IsUnifiedDrawingActive())
                {
                    return;
                }

                if (button.ChangedButton == MouseButton.Left && _selectionDragging)
                {
                    _selectionDragging = false;
                    Chart.ReleaseMouseCapture();
                    Chart.UserInputProcessor.IsEnabled = true;
                }
                return;
            }

            if (e.StagingItem.Input is MouseEventArgs mouse &&
                _selectionDragging &&
                _selectedDrawing != null &&
                mouse.LeftButton == MouseButtonState.Pressed)
            {
                if (!SourceBelongsToThisChart(mouse.OriginalSource as DependencyObject)) return;
                if (!TryGetRawChartPoint(mouse, out ScottPlot.Coordinates point)) return;

                double dx = point.X - _selectionDragStart.X;
                double dy = point.Y - _selectionDragStart.Y;
                if (Math.Abs(dx) < 1e-15 && Math.Abs(dy) < 1e-15) return;

                MoveSelectedDrawing(dx, dy);
                _selectionDragStart = point;
                _drawingSelectionMoved = true;
                mouse.Handled = true;
                Chart.Refresh();
                return;
            }
        }

        private bool IsPointOnSelectedDrawing(ScottPlot.Coordinates point)
        {
            if (_selectedDrawing == null) return false;
            double tolerance = GetDrawingHitTolerance();

            return _selectedDrawingKind switch
            {
                DrawingSelectionKind.Fibonacci => DistanceToFibonacci(point, (FibonacciDrawing)_selectedDrawing) <= tolerance,
                DrawingSelectionKind.Pitchfork => DistanceToPitchfork(point, (PitchforkDrawing)_selectedDrawing) <= tolerance,
                DrawingSelectionKind.ParallelChannel =>
                    DistancePointToSegment(point, ((ParallelChannelDrawing)_selectedDrawing).A, ((ParallelChannelDrawing)_selectedDrawing).B) <= tolerance,
                DrawingSelectionKind.Rectangle => IsPointOnRectangle(point, (RectangleDrawing)_selectedDrawing, tolerance),
                DrawingSelectionKind.Ray => DistanceToRay(point, ((RayDrawing)_selectedDrawing).X1, ((RayDrawing)_selectedDrawing).Y1, ((RayDrawing)_selectedDrawing).X2, ((RayDrawing)_selectedDrawing).Y2) <= tolerance,
                DrawingSelectionKind.TrendLine => DistancePointToSegment(point,
                    new ScottPlot.Coordinates(((TrendLineDrawing)_selectedDrawing).X1, ((TrendLineDrawing)_selectedDrawing).Y1),
                    new ScottPlot.Coordinates(((TrendLineDrawing)_selectedDrawing).X2, ((TrendLineDrawing)_selectedDrawing).Y2)) <= tolerance,
                DrawingSelectionKind.HorizontalLine => Math.Abs(point.Y - ((HorizontalLineDrawing)_selectedDrawing).Y) <= tolerance,
                DrawingSelectionKind.VerticalLine => Math.Abs(point.X - ((VerticalLineDrawing)_selectedDrawing).X) <= tolerance,
                _ => false
            };
        }

        private static bool IsPointOnRectangle(ScottPlot.Coordinates point, RectangleDrawing d, double tolerance)
        {
            double left = Math.Min(d.A.X, d.B.X), right = Math.Max(d.A.X, d.B.X);
            double bottom = Math.Min(d.A.Y, d.B.Y), top = Math.Max(d.A.Y, d.B.Y);
            return DistancePointToSegment(point, new ScottPlot.Coordinates(left, bottom), new ScottPlot.Coordinates(right, bottom)) <= tolerance ||
                   DistancePointToSegment(point, new ScottPlot.Coordinates(right, bottom), new ScottPlot.Coordinates(right, top)) <= tolerance ||
                   DistancePointToSegment(point, new ScottPlot.Coordinates(right, top), new ScottPlot.Coordinates(left, top)) <= tolerance ||
                   DistancePointToSegment(point, new ScottPlot.Coordinates(left, top), new ScottPlot.Coordinates(left, bottom)) <= tolerance;
        }

        private void ShowSelectedDrawingSettings()
        {
            var window = new ChartSettingsWindow(_settings)
            {
                Owner = Window.GetWindow(this),
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            if (window.ShowDialog() == true)
            {
                _settings = ChartSettingsManager.Current;
                RenderTechnicalDrawings();
                RenderAllFibonacciDrawings();
                RenderTextDrawings();
                Chart.Refresh();
            }
        }

        private void SetAllDrawingButtonVisuals()
        {
            SetDrawingButtonVisual(DrawingSelectButton, _activeDrawingTool == TechnicalDrawingTool.Select && !_textDrawingActive);
            SetDrawingButtonVisual(DrawingTrendLineButton, _activeDrawingTool == TechnicalDrawingTool.TrendLine);
            SetDrawingButtonVisual(DrawingHorizontalLineButton, _activeDrawingTool == TechnicalDrawingTool.HorizontalLine);
            SetDrawingButtonVisual(DrawingVerticalLineButton, _activeDrawingTool == TechnicalDrawingTool.VerticalLine);
            SetDrawingButtonVisual(DrawingRayButton, _activeDrawingTool == TechnicalDrawingTool.Ray);
            SetDrawingButtonVisual(DrawingTextButton, _textDrawingActive);
            SetDrawingButtonVisual(DrawingFibRetracementButton, (int)_activeDrawingTool == UnifiedFibRetracement);
            SetDrawingButtonVisual(DrawingFibExtensionButton, (int)_activeDrawingTool == UnifiedFibExtension);
            SetDrawingButtonVisual(DrawingParallelChannelButton, IsAdvancedDrawingTool && (int)_activeDrawingTool == 5);
            SetDrawingButtonVisual(DrawingRectangleButton, IsAdvancedDrawingTool && (int)_activeDrawingTool == 6);
            SetDrawingButtonVisual(DrawingPitchforkButton, IsAdvancedDrawingTool && (int)_activeDrawingTool == 7);
        }

        private static void SetDrawingButtonVisual(Button button, bool active)
        {
            button.Opacity = active ? 1.0 : 0.55;
            button.Background = active ? Brushes.DodgerBlue : null;
            button.Foreground = active ? Brushes.White : null;
            button.BorderBrush = active ? Brushes.DodgerBlue : null;
            button.BorderThickness = active ? new Thickness(2) : new Thickness(1);
            button.FontWeight = active ? FontWeights.Bold : FontWeights.Normal;
        }
    }
}
