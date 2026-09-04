using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private const int UnifiedFibRetracement = 8;
        private const int UnifiedFibExtension = 9;

        private sealed class FibonacciDrawing
        {
            public bool IsExtension { get; init; }
            public ScottPlot.Coordinates A { get; set; }
            public ScottPlot.Coordinates B { get; set; }
            public ScottPlot.Coordinates C { get; set; }
            public List<ScottPlot.Plottables.Scatter> Lines { get; } = new();
        }

        private readonly List<FibonacciDrawing> _fibonacciDrawings = new();
        private bool _unifiedDrawingInputAttached;
        private Window? _unifiedDrawingWindow;
        private ScottPlot.Coordinates? _unifiedFibP1;
        private ScottPlot.Coordinates? _unifiedFibP2;
        private ScottPlot.Plottables.Scatter? _unifiedFibPreview;

        private static readonly bool _unifiedDrawingInputRegistered = RegisterUnifiedDrawingInput();

        private static bool RegisterUnifiedDrawingInput()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(UnifiedDrawing_StaticLoaded));
            return true;
        }

        private static void UnifiedDrawing_StaticLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart) chart.AttachUnifiedDrawingInput();
        }

        private void AttachUnifiedDrawingInput()
        {
            if (_unifiedDrawingInputAttached) return;
            _unifiedDrawingInputAttached = true;

            InputManager.Current.PreProcessInput += UnifiedDrawing_PreProcessInput;
            DrawingFibRetracementButton.Click += UnifiedDrawing_FibRetracementClick;
            DrawingFibExtensionButton.Click += UnifiedDrawing_FibExtensionClick;

            // Fibonacci must receive chart clicks after TechnicalDrawing's preview handler.
            Chart.PreviewMouseLeftButtonDown += UnifiedDrawing_ChartLeftMouseDown;
            Chart.PreviewMouseMove += UnifiedDrawing_ChartMouseMove;

            AddHandler(Keyboard.PreviewKeyDownEvent,
                new System.Windows.Input.KeyEventHandler(UnifiedDrawing_ControlKeyDown), true);
            Chart.PreviewMouseRightButtonDown += UnifiedDrawing_ChartRightMouseDown;
            Loaded += UnifiedDrawing_Loaded;
            Unloaded += UnifiedDrawing_Unloaded;
        }

        private void UnifiedDrawing_Loaded(object sender, RoutedEventArgs e)
        {
            _unifiedDrawingWindow = Window.GetWindow(this);
            if (_unifiedDrawingWindow != null)
            {
                _unifiedDrawingWindow.PreviewKeyDown -= UnifiedDrawing_WindowKeyDown;
                _unifiedDrawingWindow.PreviewKeyDown += UnifiedDrawing_WindowKeyDown;
            }
        }

        private void UnifiedDrawing_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_unifiedDrawingWindow != null)
                _unifiedDrawingWindow.PreviewKeyDown -= UnifiedDrawing_WindowKeyDown;
            _unifiedDrawingWindow = null;
        }

        private void UnifiedDrawing_PreProcessInput(object sender, PreProcessInputEventArgs e)
        {
            if (e.StagingItem.Input is System.Windows.Input.KeyEventArgs key)
            {
                if (key.Key != Key.Escape && key.Key != Key.Cancel) return;
                if (!IsUnifiedDrawingActive() && !_textDrawingActive) return;
                CancelUnifiedDrawing();
                key.Handled = true;
                return;
            }

            // Do NOT consume left mouse input here. It is handled by the chart routed
            // events below, which prevents the Fibonacci clicks from being swallowed
            // before ChartTabView receives them.
        }

        private void UnifiedDrawing_ChartLeftMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || _textDrawingActive) return;
            if (!IsUnifiedFibonacciActive()) return;
            if (!TryGetRawChartPoint(e, out ScottPlot.Coordinates point)) return;
            UnifiedFib_MouseDown(e);
            e.Handled = true;
        }

        private void UnifiedDrawing_ChartMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_textDrawingActive || !IsUnifiedFibonacciActive()) return;
            if (_unifiedFibP1 == null) return;
            UnifiedFib_MouseMove(e);
            e.Handled = true;
        }

        private void UnifiedDrawing_ControlKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Escape && e.Key != Key.Cancel) return;
            if (!IsUnifiedDrawingActive() && !_textDrawingActive) return;
            CancelUnifiedDrawing();
            e.Handled = true;
        }

        private void UnifiedDrawing_ChartRightMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right) return;
            if (!IsUnifiedDrawingActive() && !_textDrawingActive) return;
            CancelUnifiedDrawing();
            e.Handled = true;
        }

        private void UnifiedDrawing_WindowKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Escape && e.Key != Key.Cancel) return;
            if (!IsUnifiedDrawingActive() && !_textDrawingActive) return;
            CancelUnifiedDrawing();
            e.Handled = true;
        }

        private bool SourceBelongsToThisChart(DependencyObject? source)
        {
            DependencyObject? current = source;
            while (current != null)
            {
                if (ReferenceEquals(current, Chart) || ReferenceEquals(current, this)) return true;
                current = current is System.Windows.Media.Visual visual
                    ? System.Windows.Media.VisualTreeHelper.GetParent(visual)
                    : current is FrameworkElement element ? element.Parent
                    : current is FrameworkContentElement content ? content.Parent : null;
            }
            return false;
        }

        private bool IsUnifiedFibonacciActive()
        {
            int tool = (int)_activeDrawingTool;
            return tool == UnifiedFibRetracement || tool == UnifiedFibExtension;
        }

        private bool IsUnifiedDrawingActive()
        {
            return _activeDrawingTool == TechnicalDrawingTool.Ray || IsAdvancedDrawingTool || IsUnifiedFibonacciActive();
        }

        private void UnifiedDrawing_FibRetracementClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            SetUnifiedFibonacciTool(UnifiedFibRetracement);
        }

        private void UnifiedDrawing_FibExtensionClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            SetUnifiedFibonacciTool(UnifiedFibExtension);
        }

        private void SetUnifiedFibonacciTool(int tool)
        {
            CancelUnifiedDrawing(false);
            _activeDrawingTool = (TechnicalDrawingTool)tool;
            Chart.UserInputProcessor.IsEnabled = false;
            UpdateTechnicalDrawingButtons();
            DrawingFibRetracementButton.Opacity = tool == UnifiedFibRetracement ? 1.0 : 0.55;
            DrawingFibExtensionButton.Opacity = tool == UnifiedFibExtension ? 1.0 : 0.55;
            DrawingParallelChannelButton.Opacity = 0.55;
            DrawingRectangleButton.Opacity = 0.55;
            DrawingPitchforkButton.Opacity = 0.55;
            Chart.Focusable = true;
            Chart.Focus();
            Focus();
            ChartInfoTextBlock.Text = tool == UnifiedFibRetracement
                ? $"{_symbol.Symbol} | فیبوناچی اصلاحی: نقطه اول را کلیک کنید"
                : $"{_symbol.Symbol} | فیبوناچی اکستنشن: نقطه A را کلیک کنید";
            Chart.Refresh();
        }

        private void UnifiedFib_MouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!TryGetRawChartPoint(e, out ScottPlot.Coordinates point)) return;
            point = SnapUnifiedFibPoint(point);

            if (_unifiedFibP1 == null)
            {
                _unifiedFibP1 = point;
                ChartInfoTextBlock.Text = (int)_activeDrawingTool == UnifiedFibRetracement
                    ? $"{_symbol.Symbol} | فیبوناچی اصلاحی: نقطه دوم را کلیک کنید"
                    : $"{_symbol.Symbol} | فیبوناچی اکستنشن: نقطه B را کلیک کنید";
                return;
            }

            if (_unifiedFibP2 == null)
            {
                _unifiedFibP2 = point;
                if ((int)_activeDrawingTool == UnifiedFibRetracement)
                {
                    DrawUnifiedFibRetracement();
                    ResetUnifiedFibPoints();
                    ChartInfoTextBlock.Text = $"{_symbol.Symbol} | فیبوناچی اصلاحی رسم شد";
                }
                else
                {
                    ChartInfoTextBlock.Text = $"{_symbol.Symbol} | فیبوناچی اکستنشن: نقطه C را کلیک کنید";
                }
                Chart.Refresh();
                return;
            }

            DrawUnifiedFibExtension(point);
            ResetUnifiedFibPoints();
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | فیبوناچی اکستنشن رسم شد";
            Chart.Refresh();
        }

        private void UnifiedFib_MouseMove(System.Windows.Input.MouseEventArgs e)
        {
            if (_unifiedFibP1 == null || !TryGetRawChartPoint(e, out ScottPlot.Coordinates point)) return;
            RemoveUnifiedFibPreview();
            point = SnapUnifiedFibPoint(point);
            var limits = Chart.Plot.Axes.GetLimits();

            if ((int)_activeDrawingTool == UnifiedFibRetracement)
            {
                double range = point.Y - _unifiedFibP1.Value.Y;
                double y = _unifiedFibP1.Value.Y + range * 0.618;
                _unifiedFibPreview = AddScatterLine(limits.Left, y, limits.Right, y);
            }
            else if (_unifiedFibP2 == null)
            {
                _unifiedFibPreview = AddScatterLine(_unifiedFibP1.Value.X, _unifiedFibP1.Value.Y, point.X, point.Y);
            }
            else
            {
                double y = point.Y + (_unifiedFibP2.Value.Y - _unifiedFibP1.Value.Y);
                _unifiedFibPreview = AddScatterLine(limits.Left, y, limits.Right, y);
            }
            Chart.Refresh();
        }

        private ScottPlot.Coordinates SnapUnifiedFibPoint(ScottPlot.Coordinates point)
        {
            int index = FindNearestDrawingBarIndex(point.X);
            return index >= 0 ? new ScottPlot.Coordinates(GetDrawingX(index), point.Y) : point;
        }

        private void DrawUnifiedFibRetracement()
        {
            var drawing = new FibonacciDrawing
            {
                IsExtension = false,
                A = _unifiedFibP1!.Value,
                B = _unifiedFibP2!.Value
            };
            _fibonacciDrawings.Add(drawing);
            RenderFibonacciDrawing(drawing);
        }

        private void DrawUnifiedFibExtension(ScottPlot.Coordinates c)
        {
            var drawing = new FibonacciDrawing
            {
                IsExtension = true,
                A = _unifiedFibP1!.Value,
                B = _unifiedFibP2!.Value,
                C = c
            };
            _fibonacciDrawings.Add(drawing);
            RenderFibonacciDrawing(drawing);
        }

        private void RenderFibonacciDrawing(FibonacciDrawing drawing)
        {
            RemoveFibonacciLines(drawing);
            var limits = Chart.Plot.Axes.GetLimits();
            double ab = drawing.B.Y - drawing.A.Y;
            double[] ratios = drawing.IsExtension
                ? new[] { 0.0, 0.382, 0.618, 1.0, 1.618, 2.618 }
                : new[] { 0.0, 0.236, 0.382, 0.5, 0.618, 0.786, 1.0 };

            foreach (double ratio in ratios)
            {
                double y = drawing.IsExtension
                    ? drawing.C.Y + ab * ratio
                    : drawing.B.Y - ab * ratio;
                drawing.Lines.Add(AddScatterLine(limits.Left, y, limits.Right, y));
            }
        }

        private void RenderAllFibonacciDrawings()
        {
            foreach (var drawing in _fibonacciDrawings)
                RenderFibonacciDrawing(drawing);
        }

        private void RemoveFibonacciLines(FibonacciDrawing drawing)
        {
            foreach (var line in drawing.Lines) Chart.Plot.Remove(line);
            drawing.Lines.Clear();
        }

        private void ResetUnifiedFibPoints()
        {
            _unifiedFibP1 = null;
            _unifiedFibP2 = null;
            RemoveUnifiedFibPreview();
        }

        private void RemoveUnifiedFibPreview()
        {
            if (_unifiedFibPreview != null) Chart.Plot.Remove(_unifiedFibPreview);
            _unifiedFibPreview = null;
        }

        private void CancelUnifiedDrawing(bool refresh = true)
        {
            ResetUnifiedFibPoints();
            _horizontalRayStart = null;
            RemoveHorizontalRayPreview();
            RemoveAdvancedPreview();
            _advancedDrawingP1 = null;
            _advancedDrawingP2 = null;
            RemoveTrendLinePreview();
            _trendLineStart = null;
            _textDrawingActive = false;
            Chart.ReleaseMouseCapture();
            _activeDrawingTool = TechnicalDrawingTool.Select;
            Chart.UserInputProcessor.IsEnabled = true;
            UpdateTechnicalDrawingButtons();
            DrawingParallelChannelButton.Opacity = 0.55;
            DrawingRectangleButton.Opacity = 0.55;
            DrawingPitchforkButton.Opacity = 0.55;
            DrawingFibRetracementButton.Opacity = 0.55;
            DrawingFibExtensionButton.Opacity = 0.55;
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | رسم ابزار لغو شد";
            if (refresh) Chart.Refresh();
        }
    }
}
