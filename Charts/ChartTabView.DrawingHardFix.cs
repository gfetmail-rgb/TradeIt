using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private const int HardFixFibonacciRetracement = 8;
        private const int HardFixFibonacciExtension = 9;

        private sealed class FibonacciDrawing
        {
            public int Tool { get; init; }
            public ScottPlot.Coordinates A { get; init; }
            public ScottPlot.Coordinates B { get; init; }
            public ScottPlot.Coordinates? C { get; init; }
            public readonly List<ScottPlot.Plottables.Scatter> Lines = new();
        }

        private readonly List<FibonacciDrawing> _fibonacciDrawings = new();
        private ScottPlot.Coordinates? _hardFixFibP1;
        private ScottPlot.Coordinates? _hardFixFibP2;
        private ScottPlot.Plottables.Scatter? _hardFixFibPreview;
        private Window? _hardFixWindow;
        private bool _hardFixWindowAttached;

        private static readonly bool _drawingHardFixRegistered = RegisterDrawingHardFix();

        private static bool RegisterDrawingHardFix()
        {
            // Class handlers execute before the existing instance handlers attached
            // directly to WpfPlot. This is intentional: the old bridge otherwise
            // interprets Ray and advanced tools as ordinary TrendLine input.
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                UIElement.PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler(DrawingHardFix_LeftDown));

            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                UIElement.PreviewMouseMoveEvent,
                new MouseEventHandler(DrawingHardFix_MouseMove));

            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                UIElement.PreviewMouseRightButtonDownEvent,
                new MouseButtonEventHandler(DrawingHardFix_RightDown), true);

            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                Keyboard.PreviewKeyDownEvent,
                new System.Windows.Input.KeyEventHandler(DrawingHardFix_KeyDown), true);

            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                Button.ClickEvent,
                new RoutedEventHandler(DrawingHardFix_ButtonClick), true);

            EventManager.RegisterClassHandler(
                typeof(Window),
                Keyboard.PreviewKeyDownEvent,
                new System.Windows.Input.KeyEventHandler(DrawingHardFix_WindowKeyDown), true);

            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(DrawingHardFix_Loaded));

            return true;
        }

        private static bool IsHardFixDrawingTool(ChartTabView chart)
        {
            return chart._activeDrawingTool == TechnicalDrawingTool.Ray ||
                   chart.IsAdvancedDrawingTool ||
                   (int)chart._activeDrawingTool == HardFixFibonacciRetracement ||
                   (int)chart._activeDrawingTool == HardFixFibonacciExtension;
        }

        private static void DrawingHardFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart || chart._hardFixWindowAttached)
                return;

            chart._hardFixWindow = Window.GetWindow(chart);
            if (chart._hardFixWindow == null)
                return;

            chart._hardFixWindowAttached = true;
            chart._hardFixWindow.PreviewKeyDown += chart.DrawingHardFix_WindowKeyDownInstance;
            chart._hardFixWindow.Unloaded += chart.DrawingHardFix_WindowUnloaded;
        }

        private void DrawingHardFix_WindowKeyDownInstance(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Escape && e.Key != Key.Cancel)
                return;
            if (!IsHardFixDrawingTool(this) && !_textDrawingActive)
                return;

            CancelHardFixDrawingState();
            e.Handled = true;
        }

        private void DrawingHardFix_WindowUnloaded(object? sender, RoutedEventArgs e)
        {
            if (_hardFixWindow != null)
                _hardFixWindow.PreviewKeyDown -= DrawingHardFix_WindowKeyDownInstance;
            if (_hardFixWindow != null)
                _hardFixWindow.Unloaded -= DrawingHardFix_WindowUnloaded;
            _hardFixWindow = null;
            _hardFixWindowAttached = false;
        }

        private static ChartTabView? FindChartTabView(DependencyObject? source)
        {
            DependencyObject? current = source;
            while (current != null)
            {
                if (current is ChartTabView chart)
                    return chart;

                DependencyObject? next = null;
                if (current is Visual || current is Visual3D)
                    next = VisualTreeHelper.GetParent(current);
                if (next == null && current is FrameworkElement fe)
                    next = fe.Parent;
                if (next == null && current is FrameworkContentElement fce)
                    next = fce.Parent;
                current = next;
            }
            return null;
        }

        private static void DrawingHardFix_WindowKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Escape && e.Key != Key.Cancel)
                return;

            ChartTabView? chart = FindChartTabView(e.OriginalSource as DependencyObject);
            if (chart == null || (!IsHardFixDrawingTool(chart) && !chart._textDrawingActive))
                return;

            chart.CancelHardFixDrawingState();
            e.Handled = true;
        }

        private static void DrawingHardFix_LeftDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (sender is not ChartTabView chart || chart._textDrawingActive)
                return;

            if (!IsHardFixDrawingTool(chart))
                return;

            // This class handler runs before the old WpfPlot instance handler.
            // Therefore Ray/advanced/Fibonacci never enter TechnicalDrawing_MouseDown.
            if ((int)chart._activeDrawingTool == HardFixFibonacciRetracement ||
                (int)chart._activeDrawingTool == HardFixFibonacciExtension)
            {
                chart.HardFix_FibonacciMouseDown(e);
            }
            else
            {
                chart.AdvancedDrawing_MouseDown(chart, e);
            }

            e.Handled = true;
        }

        private static void DrawingHardFix_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not ChartTabView chart || chart._textDrawingActive)
                return;
            if (!IsHardFixDrawingTool(chart))
                return;

            if ((int)chart._activeDrawingTool == HardFixFibonacciRetracement ||
                (int)chart._activeDrawingTool == HardFixFibonacciExtension)
                chart.HardFix_FibonacciMouseMove(e);
            else
                chart.AdvancedDrawing_MouseMove(chart, e);

            e.Handled = true;
        }

        private static void DrawingHardFix_RightDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right)
                return;
            if (sender is not ChartTabView chart)
                return;
            if (!IsHardFixDrawingTool(chart) && !chart._textDrawingActive)
                return;

            chart.CancelHardFixDrawingState();
            e.Handled = true;
        }

        private static void DrawingHardFix_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Escape && e.Key != Key.Cancel)
                return;
            if (sender is not ChartTabView chart)
                return;
            if (!IsHardFixDrawingTool(chart) && !chart._textDrawingActive)
                return;

            chart.CancelHardFixDrawingState();
            e.Handled = true;
        }

        private static void DrawingHardFix_ButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart)
                return;
            if (e.OriginalSource is not Button button)
                return;

            if (button == chart.DrawingFibRetracementButton)
            {
                e.Handled = true;
                chart.SetHardFixFibonacciTool(HardFixFibonacciRetracement);
            }
            else if (button == chart.DrawingFibExtensionButton)
            {
                e.Handled = true;
                chart.SetHardFixFibonacciTool(HardFixFibonacciExtension);
            }
        }

        private void SetHardFixFibonacciTool(int tool)
        {
            CancelHardFixDrawingState(false);
            _activeDrawingTool = (TechnicalDrawingTool)tool;
            Chart.UserInputProcessor.IsEnabled = false;
            DrawingSelectButton.Opacity = 0.55;
            DrawingTrendLineButton.Opacity = 0.55;
            DrawingHorizontalLineButton.Opacity = 0.55;
            DrawingVerticalLineButton.Opacity = 0.55;
            DrawingRayButton.Opacity = 0.55;
            DrawingParallelChannelButton.Opacity = 0.55;
            DrawingRectangleButton.Opacity = 0.55;
            DrawingPitchforkButton.Opacity = 0.55;
            DrawingFibRetracementButton.Opacity = tool == HardFixFibonacciRetracement ? 1.0 : 0.55;
            DrawingFibExtensionButton.Opacity = tool == HardFixFibonacciExtension ? 1.0 : 0.55;
            Chart.Focusable = true;
            Chart.Focus();
            Focus();
            ChartInfoTextBlock.Text = tool == HardFixFibonacciRetracement
                ? $"{_symbol.Symbol} | فیبوناچی اصلاحی: نقطه اول را کلیک کنید"
                : $"{_symbol.Symbol} | فیبوناچی اکستنشن: نقطه A را کلیک کنید";
            Chart.Refresh();
        }

        private void HardFix_FibonacciMouseDown(MouseButtonEventArgs e)
        {
            if (!TryGetRawChartPoint(e, out ScottPlot.Coordinates point))
                return;

            if (_hardFixFibP1 == null)
            {
                _hardFixFibP1 = SnapDrawingPoint(point);
                ChartInfoTextBlock.Text = (int)_activeDrawingTool == HardFixFibonacciRetracement
                    ? $"{_symbol.Symbol} | فیبوناچی اصلاحی: نقطه دوم را کلیک کنید"
                    : $"{_symbol.Symbol} | فیبوناچی اکستنشن: نقطه B را کلیک کنید";
                return;
            }

            if (_hardFixFibP2 == null)
            {
                _hardFixFibP2 = SnapDrawingPoint(point);
                if ((int)_activeDrawingTool == HardFixFibonacciRetracement)
                {
                    var drawing = new FibonacciDrawing
                    {
                        Tool = HardFixFibonacciRetracement,
                        A = _hardFixFibP1.Value,
                        B = _hardFixFibP2.Value
                    };
                    _fibonacciDrawings.Add(drawing);
                    AddFibonacciRetracement(drawing);
                    _hardFixFibP1 = null;
                    _hardFixFibP2 = null;
                    RemoveHardFixFibPreview();
                    ChartInfoTextBlock.Text = $"{_symbol.Symbol} | فیبوناچی اصلاحی رسم شد";
                }
                else
                {
                    ChartInfoTextBlock.Text = $"{_symbol.Symbol} | فیبوناچی اکستنشن: نقطه C را کلیک کنید";
                }
                return;
            }

            var extension = new FibonacciDrawing
            {
                Tool = HardFixFibonacciExtension,
                A = _hardFixFibP1.Value,
                B = _hardFixFibP2.Value,
                C = SnapDrawingPoint(point)
            };
            _fibonacciDrawings.Add(extension);
            AddFibonacciExtension(extension);
            _hardFixFibP1 = null;
            _hardFixFibP2 = null;
            RemoveHardFixFibPreview();
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | فیبوناچی اکستنشن رسم شد";
        }

        private void HardFix_FibonacciMouseMove(MouseEventArgs e)
        {
            if (_hardFixFibP1 == null || !TryGetRawChartPoint(e, out ScottPlot.Coordinates point))
                return;

            RemoveHardFixFibPreview();
            point = SnapDrawingPoint(point);
            var limits = Chart.Plot.Axes.GetLimits();

            if ((int)_activeDrawingTool == HardFixFibonacciRetracement)
            {
                double low = Math.Min(_hardFixFibP1.Value.Y, point.Y);
                double high = Math.Max(_hardFixFibP1.Value.Y, point.Y);
                _hardFixFibPreview = AddScatterLine(
                    limits.Left,
                    high,
                    limits.Right,
                    high);
            }
            else if (_hardFixFibP2 == null)
            {
                _hardFixFibPreview = AddScatterLine(
                    _hardFixFibP1.Value.X,
                    _hardFixFibP1.Value.Y,
                    point.X,
                    point.Y);
            }
            else
            {
                double ab = _hardFixFibP2.Value.Y - _hardFixFibP1.Value.Y;
                double[] levels = { 0.0, 0.382, 0.618, 1.0, 1.618, 2.618 };
                // Preview the 100% extension level from C.
                double y = point.Y + ab;
                _hardFixFibPreview = AddScatterLine(limits.Left, y, limits.Right, y);
            }
            Chart.Refresh();
        }

        private ScottPlot.Coordinates SnapDrawingPoint(ScottPlot.Coordinates point)
        {
            int index = FindNearestDrawingBarIndex(point.X);
            return index >= 0
                ? new ScottPlot.Coordinates(GetDrawingX(index), point.Y)
                : point;
        }

        private void AddFibonacciRetracement(FibonacciDrawing d)
        {
            var limits = Chart.Plot.Axes.GetLimits();
            double range = d.B.Y - d.A.Y;
            double[] ratios = { 0.0, 0.236, 0.382, 0.5, 0.618, 0.786, 1.0 };
            foreach (double ratio in ratios)
            {
                double y = d.B.Y - range * ratio;
                d.Lines.Add(AddScatterLine(limits.Left, y, limits.Right, y));
            }
        }

        private void AddFibonacciExtension(FibonacciDrawing d)
        {
            var limits = Chart.Plot.Axes.GetLimits();
            double ab = d.B.Y - d.A.Y;
            double c = d.C?.Y ?? d.B.Y;
            double[] ratios = { 0.0, 0.382, 0.618, 1.0, 1.618, 2.618 };
            foreach (double ratio in ratios)
            {
                double y = c + ab * ratio;
                d.Lines.Add(AddScatterLine(limits.Left, y, limits.Right, y));
            }
        }

        private void RemoveHardFixFibPreview()
        {
            if (_hardFixFibPreview != null)
                Chart.Plot.Remove(_hardFixFibPreview);
            _hardFixFibPreview = null;
        }

        private void CancelHardFixDrawingState(bool refresh = true)
        {
            _hardFixFibP1 = null;
            _hardFixFibP2 = null;
            RemoveHardFixFibPreview();
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
            if (refresh)
                Chart.Refresh();
        }
    }
}
