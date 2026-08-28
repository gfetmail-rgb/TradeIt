using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            _settings = ChartSettingsManager.Current;
            _gridVisible = _settings.GridVisible;

            ApplyGridStyle(Chart);
            ApplyGridStyle(VolumeChart);
            ApplyCrosshairStyle();

            Chart.PreviewMouseMove += CrosshairFixes_PreviewMouseMove;
            VolumeChart.PreviewMouseMove += CrosshairFixes_VolumeMouseMove;
            Loaded += ChartTabView_EnsureChartType;
            ChartTypeComboBox.SelectionChanged += ChartTypeComboBox_AxisLabelsChanged;
        }

        private void ChartTypeComboBox_AxisLabelsChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                Dispatcher.BeginInvoke(new Action(ApplyHorizontalAxisLabels), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void ChartTabView_EnsureChartType(object sender, RoutedEventArgs e)
        {
            if (ChartTypeComboBox.SelectedItem is ComboBoxItem item)
            {
                string type = item.Tag?.ToString() ?? "Candlestick";
                _chartType = type switch
                {
                    "Line" => ChartDisplayType.Line,
                    "Bar" => ChartDisplayType.Bar,
                    _ => ChartDisplayType.Candlestick
                };
            }
            else
            {
                _chartType = ChartDisplayType.Candlestick;
                ChartTypeComboBox.SelectedIndex = 0;
            }

            ApplyHorizontalAxisLabels();

            if (_bars.Count > 0)
                DrawChart();

            ApplyHorizontalAxisLabels();
        }

        private void CrosshairFixes_PreviewMouseMove(object sender, WpfMouseEventArgs e)
        {
            if (_crosshair == null || !_chartVisible || !_crosshairVisible || _bars.Count == 0)
                return;

            System.Windows.Point mouse = e.GetPosition(Chart);
            if (!TryGetChartCoordinates(Chart, mouse, out ScottPlot.Coordinates coordinates))
                return;

            int nearestIndex = FindNearestBarIndex(coordinates.X);
            if (nearestIndex < 0)
                return;

            double snappedX = GetBarX(_bars[nearestIndex], nearestIndex);
            _crosshair.Position = new ScottPlot.Coordinates(snappedX, coordinates.Y);
            _crosshair.HorizontalLine.Text = coordinates.Y.ToString("N2");
            _crosshair.HorizontalLine.LabelOppositeAxis = false;
            _crosshair.VerticalLine.Text = GetBarAxisLabel(_bars[nearestIndex], nearestIndex);
            _crosshair.VerticalLine.LabelOppositeAxis = false;
            _crosshair.IsVisible = true;
            _crosshairMouseInside = true;

            UpdateCrosshairBarInformation(nearestIndex);
            Chart.Refresh();
        }

        private void CrosshairFixes_VolumeMouseMove(object sender, WpfMouseEventArgs e)
        {
            if (_crosshair == null || !_chartVisible || !_crosshairVisible || _bars.Count == 0)
                return;

            System.Windows.Point mouse = e.GetPosition(VolumeChart);
            if (!TryGetChartCoordinates(VolumeChart, mouse, out ScottPlot.Coordinates coordinates))
                return;

            var mainLimits = Chart.Plot.Axes.GetLimits();
            if (coordinates.X < mainLimits.Left || coordinates.X > mainLimits.Right)
                return;

            int nearestIndex = FindNearestBarIndex(coordinates.X);
            if (nearestIndex < 0)
                return;

            double snappedX = GetBarX(_bars[nearestIndex], nearestIndex);
            double y = (mainLimits.Bottom + mainLimits.Top) / 2.0;

            _crosshair.Position = new ScottPlot.Coordinates(snappedX, y);
            _crosshair.HorizontalLine.Text = y.ToString("N2");
            _crosshair.VerticalLine.Text = GetBarAxisLabel(_bars[nearestIndex], nearestIndex);
            _crosshair.VerticalLine.LabelOppositeAxis = false;
            _crosshair.IsVisible = true;
            _crosshairMouseInside = true;

            UpdateCrosshairBarInformation(nearestIndex);
            Chart.Refresh();
        }

        private int FindNearestBarIndex(double x)
        {
            if (_bars.Count == 0)
                return -1;

            int nearest = 0;
            double bestDistance = double.MaxValue;

            for (int i = 0; i < _bars.Count; i++)
            {
                double barX = GetBarX(_bars[i], i);
                double distance = Math.Abs(barX - x);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = i;
                }
            }

            return nearest;
        }

        private bool HasRealTimestamp(MarketBar bar)
        {
            return bar.Timestamp.HasValue &&
                   bar.Timestamp.Value > DateTime.MinValue &&
                   bar.Timestamp.Value < DateTime.MaxValue;
        }

        private double GetBarX(MarketBar bar, int index)
        {
            return HasRealTimestamp(bar)
                ? bar.Timestamp!.Value.ToOADate()
                : new DateTime(2000, 1, 1).AddDays(index).ToOADate();
        }

        private string GetBarAxisLabel(MarketBar bar, int index)
        {
            if (!HasRealTimestamp(bar))
                return $"کندل {index + 1}";

            DateTime timestamp = bar.Timestamp!.Value;
            return timestamp.TimeOfDay == TimeSpan.Zero
                ? timestamp.ToString("yyyy/MM/dd")
                : timestamp.ToString("yyyy/MM/dd HH:mm");
        }

        private void ApplyHorizontalAxisLabels()
        {
            if (_bars.Count == 0)
                return;

            double[] positions = _bars.Select((bar, index) => GetBarX(bar, index)).ToArray();
            string[] labels = _bars.Select((bar, index) => GetBarAxisLabel(bar, index)).ToArray();

            if (positions.Length > 0)
            {
                Chart.Plot.Axes.Bottom.SetTicks(positions, labels);
            }

            Chart.Refresh();
        }

        private void UpdateCrosshairBarInformation(int index)
        {
            if (index < 0 || index >= _bars.Count)
                return;

            var bar = _bars[index];
            string dateText = GetBarAxisLabel(bar, index);

            ChartInfoTextBlock.Text =
                $"{_symbol.Symbol} | O: {bar.Open:N2}  H: {bar.High:N2}  L: {bar.Low:N2}  C: {bar.Close:N2}  V: {bar.Volume:N0} | {dateText}";
        }
    }
}