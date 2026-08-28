using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using TradeIt.Models;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _displayFixesRegistered = RegisterDisplayFixes();

        private static bool RegisterDisplayFixes()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(DisplayFixes_ClassLoaded));

            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.UnloadedEvent,
                new RoutedEventHandler(DisplayFixes_ClassUnloaded));

            return true;
        }

        private static void DisplayFixes_ClassLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart)
                return;

            chart.ConfigureDateAxisLabels();
            chart.ApplyDisplayFixes();

            chart.Chart.PreviewMouseMove -= chart.DisplayFixes_ChartMouseMove;
            chart.Chart.PreviewMouseMove += chart.DisplayFixes_ChartMouseMove;

            chart.VolumeChart.PreviewMouseMove -= chart.DisplayFixes_VolumeMouseMove;
            chart.VolumeChart.PreviewMouseMove += chart.DisplayFixes_VolumeMouseMove;
        }

        private static void DisplayFixes_ClassUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart)
                return;

            chart.Chart.PreviewMouseMove -= chart.DisplayFixes_ChartMouseMove;
            chart.VolumeChart.PreviewMouseMove -= chart.DisplayFixes_VolumeMouseMove;
        }

        private void DisplayFixes_ChartMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_crosshair == null || !_chartVisible || !_crosshairVisible || _bars.Count == 0)
                return;

            WpfPoint point = e.GetPosition(Chart);

            if (!TryGetChartCoordinates(Chart, point, out ScottPlot.Coordinates coordinates))
                return;

            int index = FindNearestBarIndex(coordinates.X);
            if (index < 0)
                return;

            double x = GetBarDateTime(_bars[index], index).ToOADate();
            double y = coordinates.Y;

            _crosshair.Position = new ScottPlot.Coordinates(x, y);
            _crosshair.IsVisible = true;
            _crosshairMouseInside = true;

            UpdateSnappedMouseInformation(index, y);
            Chart.Refresh();
        }

        private void DisplayFixes_VolumeMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_crosshair == null || !_chartVisible || !_crosshairVisible || _bars.Count == 0)
                return;

            WpfPoint point = e.GetPosition(VolumeChart);

            if (!TryGetChartCoordinates(VolumeChart, point, out ScottPlot.Coordinates coordinates))
                return;

            int index = FindNearestBarIndex(coordinates.X);
            if (index < 0)
                return;

            double x = GetBarDateTime(_bars[index], index).ToOADate();
            var limits = Chart.Plot.Axes.GetLimits();
            double y = (limits.Bottom + limits.Top) / 2.0;

            _crosshair.Position = new ScottPlot.Coordinates(x, y);
            _crosshair.IsVisible = true;
            _crosshairMouseInside = true;

            UpdateSnappedMouseInformation(index, y);
            Chart.Refresh();
        }

        private void UpdateSnappedMouseInformation(int index, double crosshairPrice)
        {
            if (index < 0 || index >= _bars.Count)
                return;

            MarketBar bar = _bars[index];
            DateTime dt = GetBarDateTime(bar, index);

            bool hasRealTimestamp = _bars.Any(b =>
                b.Timestamp.HasValue &&
                b.Timestamp.Value > DateTime.MinValue &&
                b.Timestamp.Value < DateTime.MaxValue);

            string dateText = hasRealTimestamp
                ? (dt.TimeOfDay == TimeSpan.Zero
                    ? dt.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture)
                    : dt.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture))
                : $"کندل {index + 1}";

            ChartInfoTextBlock.Text =
                $"{_symbol.Symbol} | O: {bar.Open:N2}  H: {bar.High:N2}  L: {bar.Low:N2}  C: {bar.Close:N2}  V: {bar.Volume:N0} | {dateText} | قیمت: {crosshairPrice:N2}";
        }

        private void ApplyDisplayFixes()
        {
            try
            {
                ChartSettings settings = ChartSettingsManager.Current;
                _settings = settings;
                _gridVisible = settings.GridVisible;

                ApplyCrosshairSettings(settings);
                ApplyFinancialLineWidths(settings);
                ApplyGridStyle(settings);

                Chart.Plot.FigureBackground.Color = ScottPlot.Color.FromHtml(settings.FigureBackground);
                Chart.Plot.DataBackground.Color = ScottPlot.Color.FromHtml(settings.DataBackground);
                Chart.Plot.Axes.Color(ScottPlot.Color.FromHtml(settings.AxisColor));

                VolumeChart.Plot.FigureBackground.Color = ScottPlot.Color.FromHtml(settings.FigureBackground);
                VolumeChart.Plot.DataBackground.Color = ScottPlot.Color.FromHtml(settings.DataBackground);
                VolumeChart.Plot.Axes.Color(ScottPlot.Color.FromHtml(settings.AxisColor));

                Chart.Refresh();
                VolumeChart.Refresh();
            }
            catch
            {
            }
        }

        private void ApplyCrosshairSettings(ChartSettings settings)
        {
            if (_crosshair == null)
                return;

            _crosshair.LineColor = ScottPlot.Color.FromHtml(settings.CrosshairColor);
            _crosshair.LineWidth = (float)Math.Max(0.1, settings.CrosshairLineWidth);
            _crosshair.LinePattern = ParseDisplayLinePattern(settings.CrosshairPattern);
            _crosshair.HorizontalLine.LabelOppositeAxis = true;
            _crosshair.HorizontalLine.TextAlignment = ScottPlot.Alignment.MiddleLeft;
            _crosshair.VerticalLine.LabelOppositeAxis = false;
        }

        private static ScottPlot.LinePattern ParseDisplayLinePattern(string? pattern)
        {
            return pattern?.Trim().ToLowerInvariant() switch
            {
                "solid" => ScottPlot.LinePattern.Solid,
                "dashed" => ScottPlot.LinePattern.Dashed,
                "denselydashed" => ScottPlot.LinePattern.DenselyDashed,
                "dotted" => ScottPlot.LinePattern.Dotted,
                _ => ScottPlot.LinePattern.Dotted
            };
        }

        private void ApplyFinancialLineWidths(ChartSettings settings)
        {
            foreach (var plottable in Chart.Plot.GetPlottables())
            {
                string typeName = plottable.GetType().Name;

                if (typeName.Contains("Candlestick", StringComparison.OrdinalIgnoreCase))
                {
                    SetNestedLineWidth(plottable, "RisingLineStyle", settings.CandleLineWidth);
                    SetNestedLineWidth(plottable, "FallingLineStyle", settings.CandleLineWidth);
                }
                else if (typeName.Contains("OHLC", StringComparison.OrdinalIgnoreCase) ||
                         typeName.Contains("Ohlc", StringComparison.OrdinalIgnoreCase))
                {
                    SetNestedLineWidth(plottable, "RisingStyle", settings.BarLineWidth);
                    SetNestedLineWidth(plottable, "FallingStyle", settings.BarLineWidth);
                }
                else if (typeName.Contains("Scatter", StringComparison.OrdinalIgnoreCase))
                {
                    SetDirectFloatProperty(plottable, "LineWidth", settings.LineWidth);
                }
            }
        }

        private static void SetDirectFloatProperty(object target, string propertyName, double value)
        {
            try
            {
                PropertyInfo? property = target.GetType().GetProperty(propertyName);
                if (property?.CanWrite != true)
                    return;

                if (property.PropertyType == typeof(float))
                    property.SetValue(target, (float)Math.Max(0.1, value));
                else if (property.PropertyType == typeof(double))
                    property.SetValue(target, Math.Max(0.1, value));
            }
            catch
            {
            }
        }

        private static void SetNestedLineWidth(object target, string stylePropertyName, double width)
        {
            try
            {
                PropertyInfo? styleProperty = target.GetType().GetProperty(stylePropertyName);
                object? style = styleProperty?.GetValue(target);
                PropertyInfo? widthProperty = style?.GetType().GetProperty("Width");

                if (widthProperty?.CanWrite == true)
                {
                    if (widthProperty.PropertyType == typeof(float))
                        widthProperty.SetValue(style, (float)Math.Max(0.1, width));
                    else if (widthProperty.PropertyType == typeof(double))
                        widthProperty.SetValue(style, Math.Max(0.1, width));
                }
            }
            catch
            {
            }
        }

        private void ApplyGridStyle(ChartSettings settings)
        {
            ApplyGridStyleToPlot(Chart.Plot, settings);
            ApplyGridStyleToPlot(VolumeChart.Plot, settings);
        }

        private static void ApplyGridStyleToPlot(ScottPlot.Plot plot, ChartSettings settings)
        {
            try
            {
                plot.Grid.MajorLineColor = ScottPlot.Color.FromHtml(settings.GridColor);
                plot.Grid.LinePattern = ParseDisplayLinePattern(settings.GridPattern);

                PropertyInfo? width = plot.Grid.GetType().GetProperty("LineWidth")
                    ?? plot.Grid.GetType().GetProperty("MajorLineWidth");

                if (width?.CanWrite == true)
                {
                    if (width.PropertyType == typeof(float))
                        width.SetValue(plot.Grid, (float)Math.Max(0.1, settings.GridLineWidth));
                    else if (width.PropertyType == typeof(double))
                        width.SetValue(plot.Grid, Math.Max(0.1, settings.GridLineWidth));
                }

                plot.Grid.IsVisible = settings.GridVisible;
            }
            catch
            {
            }
        }

        private void ConfigureDateAxisLabels()
        {
            try
            {
                var bottom = Chart.Plot.Axes.DateTimeTicksBottom();
                if (bottom.TickGenerator is ScottPlot.TickGenerators.DateTimeAutomatic automatic)
                    automatic.LabelFormatter = FormatChartDateTick;

                var volumeBottom = VolumeChart.Plot.Axes.DateTimeTicksBottom();
                if (volumeBottom.TickGenerator is ScottPlot.TickGenerators.DateTimeAutomatic volumeAutomatic)
                    volumeAutomatic.LabelFormatter = FormatChartDateTick;
            }
            catch
            {
            }
        }

        private string FormatChartDateTick(DateTime dateTime)
        {
            bool hasRealTimestamp = _bars.Any(b =>
                b.Timestamp.HasValue &&
                b.Timestamp.Value > DateTime.MinValue &&
                b.Timestamp.Value < DateTime.MaxValue);

            if (hasRealTimestamp)
            {
                return dateTime.TimeOfDay == TimeSpan.Zero
                    ? dateTime.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture)
                    : dateTime.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);
            }

            int nearest = FindNearestBarIndex(dateTime.ToOADate());
            return nearest >= 0 ? $"کندل {nearest + 1}" : string.Empty;
        }

        private int FindNearestBarIndex(double x)
        {
            if (_bars.Count == 0)
                return -1;

            int bestIndex = 0;
            double bestDistance = double.MaxValue;

            for (int i = 0; i < _bars.Count; i++)
            {
                double barX = GetBarDateTime(_bars[i], i).ToOADate();
                double distance = Math.Abs(barX - x);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }
    }
}
