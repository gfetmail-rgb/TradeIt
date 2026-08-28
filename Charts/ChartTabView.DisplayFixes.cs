using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using TradeIt.Models;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _displayFixesInstalled;
        private bool _axisLabelsHookInstalled;

        private bool InstallDisplayFixes()
        {
            if (_displayFixesInstalled)
                return true;

            Loaded += DisplayFixes_Loaded;
            Unloaded += DisplayFixes_Unloaded;
            _displayFixesInstalled = true;
            return true;
        }

        private void DisplayFixes_Loaded(object sender, RoutedEventArgs e)
        {
            Chart.PreviewMouseMove += DisplayFixes_MouseMove;
            VolumeChart.PreviewMouseMove += DisplayFixes_VolumeMouseMove;
            ChartSettingsManager.SettingsChanged += DisplayFixes_SettingsChanged;
            InstallAxisLabelsHook();
            ApplyDisplayFixes();
        }

        private void DisplayFixes_Unloaded(object sender, RoutedEventArgs e)
        {
            Chart.PreviewMouseMove -= DisplayFixes_MouseMove;
            VolumeChart.PreviewMouseMove -= DisplayFixes_VolumeMouseMove;
            ChartSettingsManager.SettingsChanged -= DisplayFixes_SettingsChanged;
        }

        private void DisplayFixes_SettingsChanged(object? sender, EventArgs e)
        {
            ApplyDisplayFixes();
        }

        private void ApplyDisplayFixes()
        {
            try
            {
                var current = ChartSettingsManager.Current;
                _settings = current;

                ApplyCrosshairSettings(current);
                ApplyFinancialLineWidths(current);
                ApplyGridStyle(current);
                ApplyAxisLabelMode();

                Chart.Refresh();
                if (_volumeVisible)
                    VolumeChart.Refresh();
            }
            catch
            {
                // A settings refresh must never prevent the chart from opening.
            }
        }

        private void ApplyCrosshairSettings(ChartSettings settings)
        {
            if (_crosshair == null)
                return;

            _crosshair.LineColor = ScottPlot.Color.FromHtml(settings.CrosshairColor);
            _crosshair.LineWidth = (float)Math.Max(0.1, settings.CrosshairLineWidth);
            _crosshair.LinePattern = ParseDisplayLinePattern(settings.CrosshairPattern);

            // The horizontal price label belongs at the price-axis end of the crosshair,
            // not in the upper-left information area.
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
                    widthProperty.SetValue(style, (float)Math.Max(0.1, width));
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
                    width.SetValue(plot.Grid, (float)Math.Max(0.1, settings.GridLineWidth));
            }
            catch
            {
            }
        }

        private void DisplayFixes_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_bars.Count == 0 || _crosshair == null || !_crosshair.IsVisible)
                    return;

                // The base ChartTabView converts the mouse position to chart coordinates.
                // Snap that X coordinate to the nearest actual candle before rendering.
                double rawX = _crosshair.Position.X;
                int index = FindNearestBarIndex(rawX);
                if (index < 0 || index >= _bars.Count)
                    return;

                MarketBar bar = _bars[index];
                double snappedX = GetBarDateTime(bar, index).ToOADate();

                _crosshair.Position = new ScottPlot.Coordinates(
                    snappedX,
                    bar.Close);

                string dateText = GetDisplayDateText(bar, index);

                ChartInfoTextBlock.Text =
                    $"{_symbol.Symbol}    O: {bar.Open:N2}    H: {bar.High:N2}    L: {bar.Low:N2}    C: {bar.Close:N2}    V: {bar.Volume:N0}";

                // Price is shown at the end of the horizontal crosshair on the price axis.
                _crosshair.HorizontalLine.Text = bar.Close.ToString("N2", CultureInfo.InvariantCulture);
                _crosshair.VerticalLine.Text = dateText;

                Chart.Refresh();
            }
            catch
            {
            }
        }

        private void DisplayFixes_VolumeMouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_bars.Count == 0 || _crosshair == null || !_crosshair.IsVisible)
                    return;

                double rawX = _crosshair.Position.X;
                int index = FindNearestBarIndex(rawX);
                if (index < 0 || index >= _bars.Count)
                    return;

                MarketBar bar = _bars[index];
                double snappedX = GetBarDateTime(bar, index).ToOADate();
                var mainLimits = Chart.Plot.Axes.GetLimits();
                double y = Math.Max(mainLimits.Bottom, Math.Min(mainLimits.Top, bar.Close));

                _crosshair.Position = new ScottPlot.Coordinates(snappedX, y);
                _crosshair.HorizontalLine.Text = bar.Close.ToString("N2", CultureInfo.InvariantCulture);
                _crosshair.VerticalLine.Text = GetDisplayDateText(bar, index);
                ChartInfoTextBlock.Text =
                    $"{_symbol.Symbol}    O: {bar.Open:N2}    H: {bar.High:N2}    L: {bar.Low:N2}    C: {bar.Close:N2}    V: {bar.Volume:N0}";

                Chart.Refresh();
            }
            catch
            {
            }
        }

        private int FindNearestBarIndex(double x)
        {
            if (_bars.Count == 0)
                return -1;

            double bestDistance = double.MaxValue;
            int bestIndex = -1;

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

        private static string GetDisplayDateText(MarketBar bar, int index)
        {
            if (bar.Timestamp.HasValue &&
                bar.Timestamp.Value > DateTime.MinValue &&
                bar.Timestamp.Value < DateTime.MaxValue)
            {
                return bar.Timestamp.Value.ToString("yyyy/MM/dd");
            }

            return $"کندل {index + 1}";
        }

        private void InstallAxisLabelsHook()
        {
            if (_axisLabelsHookInstalled)
                return;

            Chart.RenderManager.RenderStarting += DisplayFixes_RenderStarting;
            _axisLabelsHookInstalled = true;
        }

        private void ApplyAxisLabelMode()
        {
            // DrawChart already owns the DateTime axis generator. We only customize
            // its labels during rendering, avoiding incompatible TickGenerator changes.
        }

        private void DisplayFixes_RenderStarting(object? sender, EventArgs e)
        {
            try
            {
                if (_bars.Count == 0)
                    return;

                var ticks = Chart.Plot.Axes.Bottom.TickGenerator.Ticks;
                if (ticks == null || ticks.Length == 0)
                    return;

                bool hasRealTimestamp = _bars.Any(x =>
                    x.Timestamp.HasValue &&
                    x.Timestamp.Value > DateTime.MinValue &&
                    x.Timestamp.Value < DateTime.MaxValue);

                for (int i = 0; i < ticks.Length; i++)
                {
                    int nearest = FindNearestBarIndex(ticks[i].Position);
                    if (nearest < 0)
                        continue;

                    string label;
                    if (hasRealTimestamp)
                    {
                        DateTime dt = GetBarDateTime(_bars[nearest], nearest);
                        label = dt.TimeOfDay == TimeSpan.Zero
                            ? dt.ToString("yyyy/MM/dd")
                            : dt.ToString("yyyy/MM/dd HH:mm");
                    }
                    else
                    {
                        label = $"کندل {nearest + 1}";
                    }

                    ticks[i] = new ScottPlot.Tick(ticks[i].Position, label, ticks[i].IsMajor);
                }
            }
            catch
            {
            }
        }
    }
}
