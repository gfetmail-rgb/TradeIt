using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfMouseButtonEventHandler = System.Windows.Input.MouseButtonEventHandler;
using WpfMouseEventHandler = System.Windows.Input.MouseEventHandler;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _measurementCoordinateFixAttached;
        private static readonly bool _measurementCoordinateFixRegistered = RegisterMeasurementCoordinateFix();

        private static bool RegisterMeasurementCoordinateFix()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(MeasurementCoordinateFix_Loaded));
            return true;
        }

        private static void MeasurementCoordinateFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.AttachMeasurementCoordinateFix();
        }

        private void AttachMeasurementCoordinateFix()
        {
            if (_measurementCoordinateFixAttached)
                return;

            _measurementCoordinateFixAttached = true;
            Chart.AddHandler(UIElement.PreviewMouseMoveEvent,
                new WpfMouseEventHandler(MeasurementCoordinateFix_MouseMove), true);
            Chart.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent,
                new WpfMouseButtonEventHandler(MeasurementCoordinateFix_MouseDown), true);
        }

        private void MeasurementCoordinateFix_MouseDown(object sender, WpfMouseButtonEventArgs e)
        {
            if ((int)_activeDrawingTool != MeasurementToolValue || e.ChangedButton != MouseButton.Left)
                return;

            Point mousePosition = e.GetPosition(Chart);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if ((int)_activeDrawingTool != MeasurementToolValue)
                    return;
                if (!TryGetChartCoordinates(Chart, mousePosition, out ScottPlot.Coordinates point))
                    return;

                ScottPlot.Coordinates snapped = SnapMeasurementCoordinate(point);
                if (_measurementStart == null)
                {
                    _measurementStart = snapped;
                    _measurementLastPoint = snapped;
                }
                else
                {
                    _measurementLastPoint = snapped;
                    if (_measurementLine != null)
                        Chart.Plot.Remove(_measurementLine);
                    _measurementLine = null;
                    RenderMeasurementLine();
                }
                Chart.Refresh();
            }), DispatcherPriority.Input);
        }

        private void MeasurementCoordinateFix_MouseMove(object sender, WpfMouseEventArgs e)
        {
            if ((int)_activeDrawingTool != MeasurementToolValue || _measurementStart == null || _measurementLine != null)
                return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates point))
                return;

            ScottPlot.Coordinates snapped = SnapMeasurementCoordinate(point);
            _measurementLastPoint = snapped;

            RemoveMeasurementPreview();
            _measurementPreview = Chart.Plot.Add.ScatterLine(
                new[] { _measurementStart.Value.X, snapped.X },
                new[] { _measurementStart.Value.Y, snapped.Y });
            _measurementPreview.MarkerSize = 0;
            _measurementPreview.LineColor = ScottPlot.Color.FromHtml("#555555");
            _measurementPreview.LineWidth = 1.5f;
            _measurementPreview.LinePattern = ScottPlot.LinePattern.Dashed;

            UpdateMeasurementCoordinateFixLabel(_measurementStart.Value, snapped);
            Chart.Refresh();
        }

        private ScottPlot.Coordinates SnapMeasurementCoordinate(ScottPlot.Coordinates point)
        {
            int index;
            if (_continuousTimeAxisApplied)
            {
                index = (int)Math.Round(point.X - ContinuousChartBaseDate);
                if (index < 0 || index >= _bars.Count)
                    return point;
                return new ScottPlot.Coordinates(ContinuousChartBaseDate + index, point.Y);
            }

            index = FindNearestBarIndex(point.X);
            return index >= 0
                ? new ScottPlot.Coordinates(GetBarDateTime(_bars[index], index).ToOADate(), point.Y)
                : point;
        }

        private int MeasurementCoordinateFixBarIndex(double x)
        {
            if (_continuousTimeAxisApplied)
            {
                int index = (int)Math.Round(x - ContinuousChartBaseDate);
                return index >= 0 && index < _bars.Count ? index : -1;
            }
            return FindNearestBarIndex(x);
        }

        private void UpdateMeasurementCoordinateFixLabel(ScottPlot.Coordinates a, ScottPlot.Coordinates b)
        {
            RemoveMeasurementLabel();

            int ia = MeasurementCoordinateFixBarIndex(a.X);
            int ib = MeasurementCoordinateFixBarIndex(b.X);
            int candles = ia >= 0 && ib >= 0 ? Math.Abs(ib - ia) : 0;
            double delta = b.Y - a.Y;
            double percent = Math.Abs(a.Y) > 1e-12 ? delta / a.Y * 100.0 : double.NaN;
            string deltaText = $"{(delta >= 0 ? "+" : "")}{delta:N2}";
            string percentText = double.IsFinite(percent)
                ? $"{(percent >= 0 ? "+" : "")}{percent:N2}%"
                : "—";

            string text = $"Δ قیمت: {deltaText}  |  تغییر: {percentText}  |  فاصله: {candles:N0} کندل";
            double x = (a.X + b.X) / 2.0;
            double y = (a.Y + b.Y) / 2.0;
            var limits = Chart.Plot.Axes.GetLimits();
            double range = limits.Top - limits.Bottom;
            if (range > 0)
            {
                double margin = range * 0.04;
                y = Math.Max(limits.Bottom + margin, Math.Min(limits.Top - margin, y));
            }

            _measurementLabel = Chart.Plot.Add.Text(text, x, y);
            _measurementLabel.LabelFontSize = 12;
            _measurementLabel.LabelFontColor = ScottPlot.Color.FromHtml("#202020");
            _measurementLabel.LabelBackgroundColor = ScottPlot.Colors.White.WithAlpha(0.92);
            _measurementLabel.LabelBorderColor = ScottPlot.Color.FromHtml("#707070");
            _measurementLabel.LabelBorderWidth = 1;
            _measurementLabel.LabelPadding = 4;
            _measurementLabel.LabelAlignment = ScottPlot.Alignment.MiddleCenter;
            _measurementLabel.IsVisible = _allDrawingsVisible;
        }
    }
}
