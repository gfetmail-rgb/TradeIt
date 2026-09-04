using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private const int ArrowDrawingToolValue = 10;

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
        private ScottPlot.Coordinates? _pendingArrowStart;
        private ScottPlot.Plottables.Scatter? _arrowPreview;

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
            DrawingSelectButton.Click += ArrowDeactivateFromOtherTool;
            DrawingTrendLineButton.Click += ArrowDeactivateFromOtherTool;
            DrawingHorizontalLineButton.Click += ArrowDeactivateFromOtherTool;
            DrawingVerticalLineButton.Click += ArrowDeactivateFromOtherTool;
            DrawingRayButton.Click += ArrowDeactivateFromOtherTool;
            DrawingParallelChannelButton.Click += ArrowDeactivateFromOtherTool;
            DrawingRectangleButton.Click += ArrowDeactivateFromOtherTool;
            DrawingPitchforkButton.Click += ArrowDeactivateFromOtherTool;
            DrawingFibRetracementButton.Click += ArrowDeactivateFromOtherTool;
            DrawingFibExtensionButton.Click += ArrowDeactivateFromOtherTool;
            DrawingTextButton.Click += ArrowDeactivateFromOtherTool;
            DrawingArrowButton.AddHandler(UIElement.PreviewMouseRightButtonDownEvent,
                new MouseButtonEventHandler(ArrowSettings_RightMouseDown), true);
            HideAllDrawingsButton.Click += ArrowHideAll_Click;
            DeleteAllDrawingsButton.Click += ArrowDeleteAll_Click;
        }

        private void DrawingArrowButton_Click(object? sender, RoutedEventArgs e)
        {
            CancelArrowDrawing(false);
            _arrowDrawingActive = true;
            _activeDrawingTool = (TechnicalDrawingTool)ArrowDrawingToolValue;
            _textDrawingActive = false;
            Chart.UserInputProcessor.IsEnabled = false;
            Chart.Focusable = true;
            Chart.Focus();
            SetArrowButtonVisual(true);
            UpdateTechnicalDrawingButtons();
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | پیکان: نقطه شروع را کلیک کنید";
            Chart.Refresh();
        }

        private void ArrowDeactivateFromOtherTool(object? sender, RoutedEventArgs e)
        {
            if (ReferenceEquals(sender, DrawingArrowButton)) return;
            CancelArrowDrawing(false);
            SetArrowButtonVisual(false);
        }

        private void SetArrowButtonVisual(bool selected)
        {
            DrawingArrowButton.Background = selected
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(25, 118, 210))
                : Brushes.Transparent;
            DrawingArrowButton.Foreground = selected ? Brushes.White : Brushes.Black;
            DrawingArrowButton.BorderBrush = selected
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(25, 118, 210))
                : Brushes.Transparent;
        }

        private void ArrowDrawing_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_arrowDrawingActive || (int)_activeDrawingTool != ArrowDrawingToolValue || e.ChangedButton != MouseButton.Left) return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates point)) return;

            if (!_pendingArrowStart.HasValue)
            {
                _pendingArrowStart = point;
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | پیکان: نقطه انتهایی را کلیک کنید";
                e.Handled = true;
                return;
            }

            var start = _pendingArrowStart.Value;
            if (Math.Abs(start.X - point.X) < 1e-12 && Math.Abs(start.Y - point.Y) < 1e-12) return;
            RemoveArrowPreview();
            var drawing = new ArrowDrawing { X1 = start.X, Y1 = start.Y, X2 = point.X, Y2 = point.Y };
            _arrowDrawings.Add(drawing);
            AddArrowToChart(drawing);
            _pendingArrowStart = null;
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | پیکان رسم شد؛ پیکان بعدی را شروع کنید";
            Chart.Refresh();
            e.Handled = true;
        }

        private void ArrowDrawing_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_arrowDrawingActive || (int)_activeDrawingTool != ArrowDrawingToolValue || !_pendingArrowStart.HasValue) return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates point)) return;
            RemoveArrowPreview();
            _arrowPreview = Chart.Plot.Add.ScatterLine(
                new[] { _pendingArrowStart.Value.X, point.X },
                new[] { _pendingArrowStart.Value.Y, point.Y });
            var style = GetDrawingToolStyle("Arrow");
            _arrowPreview.MarkerSize = 0;
            _arrowPreview.LineColor = ScottPlot.Color.FromHtml(style.Color);
            _arrowPreview.LineWidth = (float)Math.Max(0.5, style.LineWidth);
            _arrowPreview.LinePattern = GetDrawingLinePattern(style.LineStyle);
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | پیکان: نقطه انتهایی را انتخاب کنید | قیمت: {point.Y:N2}";
            Chart.Refresh();
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
            if (!_allDrawingsVisible) return;
            foreach (var drawing in _arrowDrawings)
            {
                if (drawing.PlotArrow != null) Chart.Plot.Remove(drawing.PlotArrow);
                AddArrowToChart(drawing);
            }
        }

        private void RemoveArrowPreview()
        {
            if (_arrowPreview != null) Chart.Plot.Remove(_arrowPreview);
            _arrowPreview = null;
        }

        private void CancelArrowDrawing(bool refresh = true)
        {
            RemoveArrowPreview();
            _pendingArrowStart = null;
            _arrowDrawingActive = false;
            if ((int)_activeDrawingTool == ArrowDrawingToolValue)
            {
                _activeDrawingTool = TechnicalDrawingTool.Select;
                Chart.UserInputProcessor.IsEnabled = true;
            }
            SetArrowButtonVisual(false);
            if (refresh) Chart.Refresh();
        }

        private void ArrowHideAll_Click(object? sender, RoutedEventArgs e)
        {
            foreach (var drawing in _arrowDrawings)
                if (drawing.PlotArrow != null) drawing.PlotArrow.IsVisible = false;
            RemoveArrowPreview();
            if (_selectedDrawing is ArrowDrawing) ClearDrawingSelectionVisuals();
            Chart.Refresh();
        }

        private void ArrowDeleteAll_Click(object? sender, RoutedEventArgs e)
        {
            foreach (var drawing in _arrowDrawings)
                if (drawing.PlotArrow != null) Chart.Plot.Remove(drawing.PlotArrow);
            _arrowDrawings.Clear();
            _selectedDrawing = null;
            _selectedDrawingKind = DrawingSelectionKind.None;
            RemoveArrowPreview();
            ClearDrawingSelectionVisuals();
            Chart.Refresh();
        }
    }
}
