using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using TradeIt.Models;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _displayFixesRegistered = RegisterDisplayFixes();

        private static bool RegisterDisplayFixes()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent, new RoutedEventHandler(DisplayFixes_Loaded));
            EventManager.RegisterClassHandler(typeof(ChartTabView), UIElement.PreviewMouseMoveEvent, new System.Windows.Input.MouseEventHandler(DisplayFixes_PreviewMouseMove));
            return true;
        }

        private static void DisplayFixes_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart) return;
            chart.ApplyDisplayFixesNow();
            ChartSettingsManager.SettingsChanged -= chart.DisplayFixes_SettingsChanged;
            ChartSettingsManager.SettingsChanged += chart.DisplayFixes_SettingsChanged;
        }

        private void DisplayFixes_SettingsChanged(object? sender, EventArgs e)
        {
            if (Dispatcher.CheckAccess()) ApplyDisplayFixesNow();
            else Dispatcher.InvokeAsync(ApplyDisplayFixesNow);
        }

        private static void DisplayFixes_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is not ChartTabView chart || chart._crosshair == null || !chart._chartVisible || !chart._crosshairVisible || chart._bars.Count == 0) return;
            if (e.OriginalSource is not System.Windows.DependencyObject source) return;
            ScottPlot.WPF.WpfPlot? plot = FindPlot(source);
            if (plot == null || (!ReferenceEquals(plot, chart.Chart) && !ReferenceEquals(plot, chart.VolumeChart))) return;
            System.Windows.Point point = e.GetPosition(plot);
            if (!chart.TryGetChartCoordinates(plot, point, out ScottPlot.Coordinates coordinates)) return;
            int index = chart.DisplayFixes_FindNearestBarIndex(coordinates.X);
            if (index < 0 || index >= chart._bars.Count) return;
            double x = chart.GetBarDateTime(chart._bars[index], index).ToOADate();
            var limits = chart.Chart.Plot.Axes.GetLimits();
            double y = ReferenceEquals(plot, chart.Chart) ? coordinates.Y : (limits.Bottom + limits.Top) / 2.0;
            chart._crosshair.Position = new ScottPlot.Coordinates(x, y);
            chart._crosshair.IsVisible = true;
            chart._crosshairMouseInside = true;
            chart.UpdateSnappedMouseInformation(index, y);
            chart.Chart.Refresh();
            e.Handled = true;
        }

        private static ScottPlot.WPF.WpfPlot? FindPlot(System.Windows.DependencyObject source)
        {
            DependencyObject? current = source;
            while (current != null)
            {
                if (current is ScottPlot.WPF.WpfPlot plot) return plot;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private void UpdateSnappedMouseInformation(int index, double crosshairPrice)
        {
            if (index < 0 || index >= _bars.Count) return;
            MarketBar bar = _bars[index];
            bool hasRealTimestamp = _bars.Any(b => b.Timestamp.HasValue && b.Timestamp.Value > DateTime.MinValue && b.Timestamp.Value < DateTime.MaxValue);
            string dateText;
            if (hasRealTimestamp)
            {
                DateTime dt = GetBarDateTime(bar, index);
                dateText = dt.TimeOfDay == TimeSpan.Zero ? dt.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture) : dt.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);
            }
            else dateText = $"کندل {index + 1}";
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | O: {bar.Open:N2}  H: {bar.High:N2}  L: {bar.Low:N2}  C: {bar.Close:N2}  V: {bar.Volume:N0}";
            BottomInfoTextBlock.Text = dateText;
        }

        private void ApplyDisplayFixesNow()
        {
            try
            {
                _settings = ChartSettingsManager.Current;
                _gridVisible = _settings.GridVisible;
                ApplyDisplayCrosshairSettings();
                ApplyDisplayGridSettings(Chart);
                ApplyDisplayGridSettings(VolumeChart);
                ApplyDisplayBackgroundAndAxes(Chart);
                ApplyDisplayBackgroundAndAxes(VolumeChart);
                ApplyDisplaySeriesWidths();
                ConfigureDisplayDateAxis(Chart);
                ConfigureDisplayDateAxis(VolumeChart);
                Chart.Refresh();
                VolumeChart.Refresh();
            }
            catch { }
        }

        private void ApplyDisplayCrosshairSettings()
        {
            if (_crosshair == null) return;
            _crosshair.LineColor = ScottPlot.Color.FromHtml(_settings.CrosshairColor);
            _crosshair.LineWidth = (float)Math.Max(0.01, _settings.CrosshairLineWidth);
            _crosshair.LinePattern = ParseDisplayPattern(_settings.CrosshairPattern);
            _crosshair.HorizontalLine.LabelOppositeAxis = true;
            _crosshair.VerticalLine.LabelOppositeAxis = false;
        }

        private static ScottPlot.LinePattern ParseDisplayPattern(string? value) => value?.Trim().ToLowerInvariant() switch
        {
            "dotted" => ScottPlot.LinePattern.Dotted,
            "dashed" => ScottPlot.LinePattern.Dashed,
            "denselydashed" => ScottPlot.LinePattern.DenselyDashed,
            _ => ScottPlot.LinePattern.Solid
        };

        private void ApplyDisplayGridSettings(ScottPlot.WPF.WpfPlot plot)
        {
            plot.Plot.Grid.IsVisible = _settings.GridVisible;
            plot.Plot.Grid.LineColor = ScottPlot.Color.FromHtml(_settings.GridColor);
            plot.Plot.Grid.LinePattern = ParseDisplayPattern(_settings.GridPattern);
            plot.Plot.Grid.MajorLineWidth = (float)Math.Max(0.01, _settings.GridLineWidth);
            plot.Plot.Grid.MinorLineWidth = (float)Math.Max(0.01, _settings.GridLineWidth);
        }

        private void ApplyDisplayBackgroundAndAxes(ScottPlot.WPF.WpfPlot plot)
        {
            plot.Plot.FigureBackground.Color = ScottPlot.Color.FromHtml(_settings.FigureBackground);
            plot.Plot.DataBackground.Color = ScottPlot.Color.FromHtml(_settings.DataBackground);
            plot.Plot.Axes.Color(ScottPlot.Color.FromHtml(_settings.AxisColor));
        }

        private void ApplyDisplaySeriesWidths()
        {
            foreach (var plottable in Chart.Plot.GetPlottables())
            {
                if (plottable is ScottPlot.Plottables.CandlestickPlot candles)
                {
                    candles.RisingLineStyle.Width = (float)Math.Max(0.01, _settings.CandleLineWidth);
                    candles.FallingLineStyle.Width = (float)Math.Max(0.01, _settings.CandleLineWidth);
                }
                else if (plottable is ScottPlot.Plottables.OhlcPlot ohlc)
                {
                    ohlc.RisingStyle.Width = (float)Math.Max(0.01, _settings.BarLineWidth);
                    ohlc.FallingStyle.Width = (float)Math.Max(0.01, _settings.BarLineWidth);
                }
                else if (plottable is ScottPlot.Plottables.Scatter scatter)
                {
                    scatter.LineWidth = (float)Math.Max(0.01, _settings.LineWidth);
                }
            }
        }

        private void ConfigureDisplayDateAxis(ScottPlot.WPF.WpfPlot plot)
        {
            bool hasRealTimestamp = _bars.Any(b => b.Timestamp.HasValue && b.Timestamp.Value > DateTime.MinValue && b.Timestamp.Value < DateTime.MaxValue);
            try
            {
                if (hasRealTimestamp)
                {
                    var axis = plot.Plot.Axes.DateTimeTicksBottom();
                    if (axis.TickGenerator is ScottPlot.TickGenerators.DateTimeAutomatic auto)
                        auto.LabelFormatter = dt => dt.TimeOfDay == TimeSpan.Zero ? dt.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture) : dt.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);
                }
                else
                {
                    int count = _bars.Count;
                    if (count == 0) return;
                    int step = Math.Max(1, count / 8);
                    var positions = new System.Collections.Generic.List<double>();
                    var labels = new System.Collections.Generic.List<string>();
                    for (int i = 0; i < count; i += step)
                    {
                        positions.Add(GetBarDateTime(_bars[i], i).ToOADate());
                        labels.Add($"کندل {i + 1}");
                    }
                    if (positions.Count == 0 || positions[^1] != GetBarDateTime(_bars[count - 1], count - 1).ToOADate())
                    {
                        positions.Add(GetBarDateTime(_bars[count - 1], count - 1).ToOADate());
                        labels.Add($"کندل {count}");
                    }
                    plot.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(positions.ToArray(), labels.ToArray());
                }
            }
            catch { }
        }

        private int DisplayFixes_FindNearestBarIndex(double x)
        {
            if (_bars.Count == 0) return -1;
            int bestIndex = 0;
            double bestDistance = double.MaxValue;
            for (int i = 0; i < _bars.Count; i++)
            {
                double barX = GetBarDateTime(_bars[i], i).ToOADate();
                double distance = Math.Abs(barX - x);
                if (distance < bestDistance) { bestDistance = distance; bestIndex = i; }
            }
            return bestIndex;
        }
    }
}
