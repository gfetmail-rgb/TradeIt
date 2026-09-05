using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfMouseButtonEventHandler = System.Windows.Input.MouseButtonEventHandler;
using WpfMouseEventHandler = System.Windows.Input.MouseEventHandler;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private const int MeasurementToolValue = 11;

        private bool _measurementEventsAttached;
        private ScottPlot.Coordinates? _measurementStart;
        private ScottPlot.Coordinates? _measurementLastPoint;
        private ScottPlot.Plottables.Scatter? _measurementPreview;
        private ScottPlot.Plottables.Scatter? _measurementLine;
        private ScottPlot.Plottables.Text? _measurementLabel;

        private static readonly bool _measurementToolRegistered = RegisterMeasurementToolHandling();

        private static bool RegisterMeasurementToolHandling()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(MeasurementTool_Loaded));
            return true;
        }

        private static void MeasurementTool_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.AttachMeasurementToolHandling();
        }

        private void AttachMeasurementToolHandling()
        {
            if (_measurementEventsAttached)
                return;

            _measurementEventsAttached = true;

            // The ruler owns mouse interaction while active. handledEventsToo is
            // required because ScottPlot may consume the routed mouse event first.
            Chart.AddHandler(
                UIElement.PreviewMouseLeftButtonDownEvent,
                new WpfMouseButtonEventHandler(MeasurementTool_ChartMouseDown),
                true);
            Chart.AddHandler(
                UIElement.PreviewMouseMoveEvent,
                new WpfMouseEventHandler(MeasurementTool_ChartMouseMove),
                true);
            Chart.AddHandler(
                UIElement.PreviewMouseRightButtonDownEvent,
                new WpfMouseButtonEventHandler(MeasurementTool_ChartRightMouseDown),
                true);

            DrawingSelectButton.Click += MeasurementTool_DeactivateFromOtherTool;
            DrawingTrendLineButton.Click += MeasurementTool_DeactivateFromOtherTool;
            DrawingArrowButton.Click += MeasurementTool_DeactivateFromOtherTool;
            DrawingHorizontalLineButton.Click += MeasurementTool_DeactivateFromOtherTool;
            DrawingVerticalLineButton.Click += MeasurementTool_DeactivateFromOtherTool;
            DrawingRayButton.Click += MeasurementTool_DeactivateFromOtherTool;
            DrawingParallelChannelButton.Click += MeasurementTool_DeactivateFromOtherTool;
            DrawingRectangleButton.Click += MeasurementTool_DeactivateFromOtherTool;
            DrawingPitchforkButton.Click += MeasurementTool_DeactivateFromOtherTool;
            DrawingFibRetracementButton.Click += MeasurementTool_DeactivateFromOtherTool;
            DrawingFibExtensionButton.Click += MeasurementTool_DeactivateFromOtherTool;
            DrawingTextButton.Click += MeasurementTool_DeactivateFromOtherTool;
            HideAllDrawingsButton.Click += MeasurementTool_HideAll;
            DeleteAllDrawingsButton.Click += MeasurementTool_DeleteAll;
            Chart.Plot.RenderManager.RenderStarting += MeasurementTool_RenderStarting;
        }

        private void MeasurementToolButton_Click(object sender, RoutedEventArgs e)
        {
            AttachMeasurementToolHandling();
            RemoveMeasurementPreview();
            RemoveMeasurementPlotOnly();
            RemoveMeasurementLabel();
            _measurementStart = null;
            _measurementLastPoint = null;
            _activeDrawingTool = (TechnicalDrawingTool)MeasurementToolValue;
            _textDrawingActive = false;

            // ScottPlot's default LeftClickDragPan must not compete with the
            // ruler's two-click state machine. The ruler receives WPF mouse events
            // directly (handledEventsToo=true), so disabling the ScottPlot input
            // processor does not prevent the ruler from receiving them.
            Chart.UserInputProcessor.IsEnabled = false;
            Chart.Focusable = true;
            Chart.Focus();
            SetMeasurementButtonVisual(true);
            UpdateTechnicalDrawingButtons();
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | خط‌کش: نقطه اول را کلیک کنید";
            Chart.Refresh();
        }

        private void SetMeasurementButtonVisual(bool selected)
        {
            DrawingMeasurementButton.Background = selected
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(25, 118, 210))
                : System.Windows.Media.Brushes.Transparent;
            DrawingMeasurementButton.Foreground = selected
                ? System.Windows.Media.Brushes.White
                : System.Windows.Media.Brushes.Black;
            DrawingMeasurementButton.BorderBrush = selected
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(25, 118, 210))
                : System.Windows.Media.Brushes.Transparent;
        }

        private void MeasurementTool_DeactivateFromOtherTool(object? sender, RoutedEventArgs e)
        {
            if ((int)_activeDrawingTool == MeasurementToolValue)
                DeactivateMeasurementTool(false);
        }

        private void MeasurementTool_ChartMouseDown(object sender, WpfMouseButtonEventArgs e)
        {
            if ((int)_activeDrawingTool != MeasurementToolValue || e.ChangedButton != MouseButton.Left)
                return;

            e.Handled = true;
            MeasurementTool_MouseDown(sender, e);
        }

        private void MeasurementTool_ChartMouseMove(object sender, WpfMouseEventArgs e)
        {
            if ((int)_activeDrawingTool != MeasurementToolValue)
                return;

            e.Handled = true;
            MeasurementTool_MouseMove(sender, e);
        }

        private void MeasurementTool_ChartRightMouseDown(object sender, WpfMouseButtonEventArgs e)
        {
            if ((int)_activeDrawingTool != MeasurementToolValue || e.ChangedButton != MouseButton.Right)
                return;

            e.Handled = true;
            DeactivateMeasurementTool(true);
        }

        private void MeasurementTool_MouseDown(object sender, WpfMouseButtonEventArgs e)
        {
            if ((int)_activeDrawingTool != MeasurementToolValue || e.ChangedButton != MouseButton.Left)
                return;

            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates point))
                return;

            if (_measurementStart == null)
            {
                _measurementStart = SnapMeasurementX(point);
                _measurementLastPoint = _measurementStart;
                RemoveMeasurementPreview();
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | خط‌کش: نقطه دوم را کلیک کنید";
                return;
            }

            if (_measurementLine == null)
            {
                _measurementLastPoint = SnapMeasurementX(point);
                RemoveMeasurementPreview();
                RenderMeasurementLine();
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | خط‌کش: اندازه‌گیری انجام شد؛ برای خاموش کردن خط‌کش کلیک کنید";
                Chart.Refresh();
                return;
            }

            DeactivateMeasurementTool(true);
        }

        private void MeasurementTool_MouseMove(object sender, WpfMouseEventArgs e)
        {
            if ((int)_activeDrawingTool != MeasurementToolValue || _measurementStart == null)
                return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates point))
                return;

            point = SnapMeasurementX(point);
            _measurementLastPoint = point;

            if (_measurementLine == null)
            {
                RemoveMeasurementPreview();
                _measurementPreview = Chart.Plot.Add.ScatterLine(
                    new[] { _measurementStart.Value.X, point.X },
                    new[] { _measurementStart.Value.Y, point.Y });
                _measurementPreview.MarkerSize = 0;
                _measurementPreview.LineColor = ScottPlot.Color.FromHtml("#666666");
                _measurementPreview.LineWidth = 1;
                _measurementPreview.LinePattern = ScottPlot.LinePattern.Dotted;
                UpdateMeasurementLabelPreview(_measurementStart.Value, point);
                Chart.Refresh();
            }
        }

        private ScottPlot.Coordinates SnapMeasurementX(ScottPlot.Coordinates point)
        {
            int index = FindNearestBarIndex(point.X);
            return index >= 0
                ? new ScottPlot.Coordinates(GetBarDateTime(_bars[index], index).ToOADate(), point.Y)
                : point;
        }

        private int GetMeasurementCandleDistance(ScottPlot.Coordinates a, ScottPlot.Coordinates b)
        {
            int ia = FindNearestBarIndex(a.X);
            int ib = FindNearestBarIndex(b.X);
            return ia >= 0 && ib >= 0 ? Math.Abs(ib - ia) : 0;
        }

        private static double GetMeasurementPercent(ScottPlot.Coordinates a, ScottPlot.Coordinates b)
        {
            if (Math.Abs(a.Y) < 1e-12) return double.NaN;
            return (b.Y - a.Y) / a.Y * 100.0;
        }

        private void UpdateMeasurementLabelPreview(ScottPlot.Coordinates a, ScottPlot.Coordinates b)
        {
            RemoveMeasurementLabel();
            int candles = GetMeasurementCandleDistance(a, b);
            double percent = GetMeasurementPercent(a, b);
            string percentText = double.IsNaN(percent) || double.IsInfinity(percent)
                ? "درصد: —"
                : $"{(percent >= 0 ? "+" : "")}{percent:N2}%";
            double labelX = (a.X + b.X) / 2.0;
            double top = Math.Max(a.Y, b.Y);
            double offset = Math.Max(Math.Abs(a.Y - b.Y) * 0.08, Math.Abs(top) * 0.012);
            double labelY = top + offset;

            _measurementLabel = Chart.Plot.Add.Text($"{candles:N0} کندل | {percentText}", labelX, labelY);
            _measurementLabel.LabelFontSize = 12;
            _measurementLabel.LabelFontColor = ScottPlot.Color.FromHtml("#202020");
            _measurementLabel.LabelBackgroundColor = ScottPlot.Colors.White.WithAlpha(0.90);
            _measurementLabel.LabelBorderColor = ScottPlot.Color.FromHtml("#707070");
            _measurementLabel.LabelBorderWidth = 1;
            _measurementLabel.LabelPadding = 4;
            _measurementLabel.LabelAlignment = ScottPlot.Alignment.LowerCenter;
            _measurementLabel.IsVisible = _allDrawingsVisible;
        }

        private void RenderMeasurementLine()
        {
            RemoveMeasurementPlotOnly();
            if (_measurementStart == null || _measurementLastPoint == null)
                return;

            var a = _measurementStart.Value;
            var b = _measurementLastPoint.Value;
            _measurementLine = Chart.Plot.Add.ScatterLine(new[] { a.X, b.X }, new[] { a.Y, b.Y });
            _measurementLine.MarkerSize = 0;
            _measurementLine.LineColor = ScottPlot.Color.FromHtml("#505050");
            _measurementLine.LineWidth = 1.5f;
            _measurementLine.LinePattern = ScottPlot.LinePattern.Solid;
            _measurementLine.IsVisible = _allDrawingsVisible;
            UpdateMeasurementLabelPreview(a, b);
        }

        private void RemoveMeasurementPlotOnly()
        {
            if (_measurementLine != null) Chart.Plot.Remove(_measurementLine);
            _measurementLine = null;
        }

        private void RemoveMeasurementPreview()
        {
            if (_measurementPreview != null) Chart.Plot.Remove(_measurementPreview);
            _measurementPreview = null;
        }

        private void RemoveMeasurementLabel()
        {
            if (_measurementLabel != null) Chart.Plot.Remove(_measurementLabel);
            _measurementLabel = null;
        }

        private void DeactivateMeasurementTool(bool refresh)
        {
            RemoveMeasurementPreview();
            RemoveMeasurementPlotOnly();
            RemoveMeasurementLabel();
            _measurementStart = null;
            _measurementLastPoint = null;
            _activeDrawingTool = TechnicalDrawingTool.Select;
            Chart.UserInputProcessor.IsEnabled = true;
            SetMeasurementButtonVisual(false);
            UpdateTechnicalDrawingButtons();
            if (refresh)
            {
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | خط‌کش خاموش شد";
                Chart.Refresh();
            }
        }

        private void MeasurementTool_HideAll(object? sender, RoutedEventArgs e)
        {
            if (_measurementLine != null) _measurementLine.IsVisible = _allDrawingsVisible;
            if (_measurementLabel != null) _measurementLabel.IsVisible = _allDrawingsVisible;
        }

        private void MeasurementTool_DeleteAll(object? sender, RoutedEventArgs e)
        {
            RemoveMeasurementPreview();
            RemoveMeasurementPlotOnly();
            RemoveMeasurementLabel();
            _measurementStart = null;
            _measurementLastPoint = null;
        }

        private void MeasurementTool_RenderStarting(object? sender, ScottPlot.RenderPack e)
        {
            if (_measurementStart == null || _measurementLastPoint == null || _measurementLine == null)
                return;

            if (!Chart.Plot.GetPlottables().Contains(_measurementLine))
                RenderMeasurementLine();
        }
    }
}
