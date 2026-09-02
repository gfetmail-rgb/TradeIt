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
        private int _displayFixesLastBarIndex = -1;

        private static bool RegisterDisplayFixes()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent, new RoutedEventHandler(DisplayFixes_Loaded));
            EventManager.RegisterClassHandler(typeof(ChartTabView), UIElement.PreviewMouseMoveEvent, new System.Windows.Input.MouseEventHandler(DisplayFixes_PreviewMouseMove));
            return true;
        }

        private static void DisplayFixes_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart) return;
            chart._displayFixesLastBarIndex = -1;
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

            // IMPORTANT:
            // Timestamp-less charts use DateTime/OADate values only as synthetic X coordinates.
            // ScottPlot's DateTime coordinate conversion can throw when its DateTime tick generator
            // is not ready yet (typically while the chart is being initialized). Do not call
            // Plot.GetCoordinates() in that case. Calculate the bar index directly from the
            // current X limits and mouse position, then snap the crosshair to that bar.
            bool hasRealTimestamp = chart._bars.Any(b =>
                b.Timestamp.HasValue &&
                b.Timestamp.Value > DateTime.MinValue &&
                b.Timestamp.Value < DateTime.MaxValue);

            if (!hasRealTimestamp)
            {
                int index = chart.DisplayFixes_FindNearestBarIndexFromMouseX(plot, point.X);
                if (index < 0 || index >= chart._bars.Count) return;

                double x = chart.GetBarDateTime(chart._bars[index], index).ToOADate();
                double y = chart.DisplayFixes_GetMouseYFromPixel(plot, point.Y);

                chart._crosshair.Position = new ScottPlot.Coordinates(x, y);
                chart._crosshair.IsVisible = true;
                chart._crosshairMouseInside = true;

                chart._crosshair.HorizontalLine.LabelOppositeAxis = false;
                chart._crosshair.VerticalLine.LabelOppositeAxis = false;
                chart._crosshair.HorizontalLine.LabelAlignment = ScottPlot.Alignment.MiddleRight;
                chart._crosshair.VerticalLine.LabelAlignment = ScottPlot.Alignment.LowerCenter;

                chart._crosshair.VerticalLine.Text = $"کندل {index + 1}";
                chart._crosshair.HorizontalLine.Text = y.ToString("N2", CultureInfo.InvariantCulture);
                chart.UpdateSnappedMouseInformation(index, y);
                chart.Chart.Refresh();

                // Prevent ChartTabView.xaml.cs from entering TryGetChartCoordinates()
                // for this event. That is the path which can trigger the DateTime generator exception.
                e.Handled = true;
                return;
            }

            if (!chart.TryGetChartCoordinates(plot, point, out ScottPlot.Coordinates coordinates)) return;

            int realIndex = chart.DisplayFixes_FindNearestBarIndex(coordinates.X);
            if (realIndex < 0 || realIndex >= chart._bars.Count) return;

            double realX = chart.GetBarDateTime(chart._bars[realIndex], realIndex).ToOADate();
            double realY = coordinates.Y;
            chart._crosshair.Position = new ScottPlot.Coordinates(realX, realY);
            chart._crosshair.IsVisible = true;
            chart._crosshairMouseInside = true;

            chart._crosshair.HorizontalLine.LabelOppositeAxis = false;
            chart._crosshair.VerticalLine.LabelOppositeAxis = false;
            chart._crosshair.HorizontalLine.LabelAlignment = ScottPlot.Alignment.MiddleRight;
            chart._crosshair.VerticalLine.LabelAlignment = ScottPlot.Alignment.LowerCenter;

            if (realIndex != chart._displayFixesLastBarIndex)
            {
                chart._displayFixesLastBarIndex = realIndex;
                chart._crosshair.VerticalLine.Text = chart.DisplayFixes_GetCrosshairXLabel(realIndex);
                chart.UpdateSnappedMouseInformation(realIndex, realY);
            }

            chart._crosshair.HorizontalLine.Text = realY.ToString("N2", CultureInfo.InvariantCulture);
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

        private int DisplayFixes_FindNearestBarIndexFromMouseX(ScottPlot.WPF.WpfPlot plot, double mouseX)
        {
            if (_bars.Count == 0 || plot.ActualWidth <= 0) return -1;

            var limits = plot.Plot.Axes.GetLimits();
            double xRange = limits.Right - limits.Left;
            if (xRange <= 0) return -1;

            // The plot control includes axis margins. Using its full width is intentionally
            // conservative here; the result is immediately snapped to the nearest real bar.
            double x = limits.Left + (mouseX / plot.ActualWidth) * xRange;
            return DisplayFixes_FindNearestBarIndex(x);
        }

        private static double DisplayFixes_GetMouseYFromPixel(ScottPlot.WPF.WpfPlot plot, double mouseY)
        {
            if (plot.ActualHeight <= 0) return 0;

            var limits = plot.Plot.Axes.GetLimits();
            double yRange = limits.Top - limits.Bottom;
            if (yRange <= 0) return limits.Bottom;

            double normalized = 1.0 - Math.Clamp(mouseY / plot.ActualHeight, 0.0, 1.0);
            return limits.Bottom + normalized * yRange;
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
            else
            {
                dateText = $"کندل {index + 1}";
            }

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
                ApplyDisplayCrosshairLayout();
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
            _crosshair.HorizontalLine.LabelOppositeAxis = false;
            _crosshair.VerticalLine.LabelOppositeAxis = false;
            _crosshair.HorizontalLine.LabelAlignment = ScottPlot.Alignment.MiddleRight;
            _crosshair.VerticalLine.LabelAlignment = ScottPlot.Alignment.LowerCenter;
        }

        private void ApplyDisplayCrosshairLayout()
        {
            Chart.Plot.Axes.Left.MinimumSize = Math.Max(85, Chart.Plot.Axes.Left.MinimumSize);
            Chart.Plot.Axes.Bottom.MinimumSize = Math.Max(38, Chart.Plot.Axes.Bottom.MinimumSize);
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
                var axis = plot.Plot.Axes.DateTimeTicksBottom();

                if (hasRealTimestamp)
                {
                    if (axis.TickGenerator is ScottPlot.TickGenerators.DateTimeAutomatic auto)
                    {
                        auto.LabelFormatter = dt =>
                            dt.TimeOfDay == TimeSpan.Zero
                                ? dt.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture)
                                : dt.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);
                    }
                }
                else
                {
                    var manualTicks = new ScottPlot.TickGenerators.DateTimeManual();
                    int count = _bars.Count;
                    int step = Math.Max(1, count / 8);

                    for (int i = 0; i < count; i += step)
                    {
                        DateTime dt = GetBarDateTime(_bars[i], i);
                        manualTicks.AddMajor(dt, $"کندل {i + 1}");
                    }

                    if (count > 0)
                    {
                        DateTime last = GetBarDateTime(_bars[count - 1], count - 1);
                        manualTicks.AddMajor(last, $"کندل {count}");
                    }

                    axis.TickGenerator = manualTicks;
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

        private string DisplayFixes_GetCrosshairXLabel(int index)
        {
            if (index < 0 || index >= _bars.Count) return string.Empty;
            MarketBar bar = _bars[index];
            if (bar.Timestamp.HasValue && bar.Timestamp.Value > DateTime.MinValue && bar.Timestamp.Value < DateTime.MaxValue)
            {
                DateTime dt = GetBarDateTime(bar, index);
                return dt.TimeOfDay == TimeSpan.Zero ? dt.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture) : dt.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);
            }
            return $"کندل {index + 1}";
        }
    }
}
