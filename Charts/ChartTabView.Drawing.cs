using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        // Drawing button visual state
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

        // Drawing cancellation and arrow cleanup
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

        // Drawing selection behavior
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
            if (sender is ChartTabView chart)
            {
                chart.AttachDrawingSelectionBehaviorFix();
                chart.Dispatcher.BeginInvoke(new Action(chart.DisableLegacyDrawingSelectionHandlers), DispatcherPriority.Input);
            }
        }

        private void DisableLegacyDrawingSelectionHandlers()
        {
            Chart.PreviewMouseLeftButtonDown -= DrawingSelection_MouseDown;
            Chart.PreviewMouseMove -= DrawingSelection_MouseMove;
            Chart.PreviewMouseLeftButtonUp -= DrawingSelection_MouseUp;
        }

        private void AttachDrawingSelectionBehaviorFix()
        {
            if (_drawingSelectionBehaviorFixAttached) return;
            _drawingSelectionBehaviorFixAttached = true;

            DisableLegacyDrawingSelectionHandlers();

            Chart.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent,
                new System.Windows.Input.MouseButtonEventHandler(DrawingSelectionBehaviorFix_MouseDown), true);
            Chart.AddHandler(UIElement.PreviewMouseMoveEvent,
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
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates chartPoint)) return;

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

            if (TrySelectDrawing(chartPoint))
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
                if (TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates textPoint))
                {
                    if (MoveSelectedText(textPoint))
                    {
                        e.Handled = true;
                        Chart.Refresh();
                    }
                }
                return;
            }

            if (!_selectionDragging || _selectedDrawing == null || _activeDrawingHandle == null || e.LeftButton != MouseButtonState.Pressed)
                return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates drawingPoint)) return;

            if (Math.Abs(drawingPoint.X - _selectionDragStart.X) < 1e-15 &&
                Math.Abs(drawingPoint.Y - _selectionDragStart.Y) < 1e-15)
                return;

            _selectionMouseMoved = true;
            DrawingHandleKind handleKind = _activeDrawingHandle.Value;
            if (MoveSelectedHandle(handleKind, drawingPoint))
            {
                _selectionDragStart = drawingPoint;
                _activeDrawingHandle = handleKind;
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
            Chart.UserInputProcessor.IsEnabled = true;
            e.Handled = true;
            Chart.Refresh();
        }

        private void DrawingSelectionBehaviorFix_RightMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right || _activeDrawingTool != TechnicalDrawingTool.Select)
                return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates point)) return;

            if (_textSelection != null && IsPointOnSelectedText(point))
            {
                ShowTextSelectionContextMenu();
                e.Handled = true;
                return;
            }

            if (_selectedDrawing == null || !IsSelectedDrawingAtPoint(point)) return;

            var menu = new ContextMenu();
            var deleteItem = new MenuItem { Header = "حذف" };
            deleteItem.Click += (_, _) => DeleteSelectedDrawing();
            menu.Items.Add(deleteItem);
            menu.IsOpen = true;
            e.Handled = true;
        }

        private bool IsSelectedDrawingAtPoint(ScottPlot.Coordinates point)
        {
            if (_selectedDrawing == null) return false;

            var candidates = GetDrawingCandidates(point);
            foreach (var candidate in candidates)
            {
                if (candidate.Kind == _selectedDrawingKind && ReferenceEquals(candidate.Drawing, _selectedDrawing))
                    return true;
            }

            return false;
        }

        // Drawing initialization
        private static readonly bool _drawingToolsRegistered = RegisterDrawingToolsHandling();

        private static bool RegisterDrawingToolsHandling()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(DrawingTools_Loaded));
            return true;
        }

        private static void DrawingTools_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart)
                return;

            chart.InitializeTechnicalDrawingHandling();
            chart.InitializeTextDrawingHandling();
            chart.AttachAdvancedDrawingTools();
            chart.AttachUnifiedDrawingInput();
            chart.InitializeDrawingCursorHandling();
        }
    }
}
