using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private sealed class ArrowDrawing
        {
            public double X1 { get; set; }
            public double Y1 { get; set; }
            public double X2 { get; set; }
            public double Y2 { get; set; }
            public ScottPlot.Plottables.Arrow? PlotArrow { get; set; }
        }

        private readonly System.Collections.Generic.List<ArrowDrawing> _arrowDrawings = new();
        private bool _arrowDrawingActive;
        private bool _arrowEventsAttached;

        private static readonly bool _arrowInitialViewRegistered = RegisterArrowInitialViewHandling();

        private static bool RegisterArrowInitialViewHandling()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(ArrowInitialView_Loaded));
            return true;
        }

        private static void ArrowInitialView_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart) return;
            chart.AttachArrowHandling();
        }

        private void AttachArrowHandling()
        {
            if (_arrowEventsAttached) return;
            _arrowEventsAttached = true;
            DrawingArrowButton.Click += DrawingArrowButton_Click;
            Chart.PreviewMouseLeftButtonDown += ArrowDrawing_MouseDown;
            Chart.PreviewMouseMove += ArrowDrawing_MouseMove;
            Chart.PreviewMouseLeftButtonUp += ArrowDrawing_MouseUp;
            DrawingArrowButton.AddHandler(UIElement.PreviewMouseRightButtonDownEvent,
                new MouseButtonEventHandler(ArrowSettings_RightMouseDown), true);
        }

        private void DrawingArrowButton_Click(object? sender, RoutedEventArgs e)
        {
            _arrowDrawingActive = true;
            _pendingArrowStart = null;
            _activeDrawingTool = TechnicalDrawingTool.Select;
            _textDrawingActive = false;
            Chart.UserInputProcessor.IsEnabled = false;
            Chart.Focusable = true;
            Chart.Focus();
            UpdateTechnicalDrawingButtons();
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | پیکان: نقطه شروع را کلیک کنید";
            Chart.Refresh();
        }

        private void ArrowDrawing_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_arrowDrawingActive || e.ChangedButton != MouseButton.Left) return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates point)) return;

            if (!HasPendingArrowStart())
            {
                _pendingArrowStart = point;
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | پیکان: نقطه انتهایی را کلیک کنید";
                e.Handled = true;
                return;
            }

            var start = _pendingArrowStart!.Value;
            if (Math.Abs(start.X - point.X) < 1e-12 && Math.Abs(start.Y - point.Y) < 1e-12) return;
            var drawing = new ArrowDrawing { X1 = start.X, Y1 = start.Y, X2 = point.X, Y2 = point.Y };
            _arrowDrawings.Add(drawing);
            AddArrowToChart(drawing);
            _pendingArrowStart = null;
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | پیکان رسم شد؛ پیکان بعدی را شروع کنید";
            Chart.Refresh();
            e.Handled = true;
        }

        private ScottPlot.Coordinates? _pendingArrowStart;

        private bool HasPendingArrowStart() => _pendingArrowStart.HasValue;

        private void ArrowDrawing_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_arrowDrawingActive || !_pendingArrowStart.HasValue) return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates point)) return;
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | پیکان: نقطه انتهایی را انتخاب کنید | قیمت: {point.Y:N2}";
        }

        private void ArrowDrawing_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) { }

        private void ArrowSettings_RightMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right) return;
            ShowDrawingToolSettings("Arrow", "پیکان");
            e.Handled = true;
        }

        private void AddArrowToChart(ArrowDrawing drawing)
        {
            var style = GetDrawingToolStyle("Arrow");
            var arrow = Chart.Plot.Add.Arrow(
                new ScottPlot.Coordinates(drawing.X1, drawing.Y1),
                new ScottPlot.Coordinates(drawing.X2, drawing.Y2));
            arrow.ArrowLineColor = ScottPlot.Color.FromHtml(style.Color);
            arrow.ArrowFillColor = ScottPlot.Color.FromHtml(style.Color);
            arrow.ArrowLineWidth = (float)Math.Max(0.5, style.LineWidth);
            arrow.ArrowWidth = (float)Math.Max(1.0, style.LineWidth * 2.5);
            arrow.ArrowheadLength = 12;
            arrow.ArrowheadAxisLength = 12;
            arrow.ArrowheadWidth = 8;
            arrow.ArrowStyle.LineStyle.Pattern = style.LineStyle == "Dash"
                ? ScottPlot.LinePattern.Dashed
                : style.LineStyle == "Dot"
                    ? ScottPlot.LinePattern.Dotted
                    : ScottPlot.LinePattern.Solid;
            drawing.PlotArrow = arrow;
        }

        private void RenderArrowDrawings()
        {
            foreach (var drawing in _arrowDrawings)
            {
                if (drawing.PlotArrow != null) Chart.Plot.Remove(drawing.PlotArrow);
                AddArrowToChart(drawing);
            }
        }
    }
}
