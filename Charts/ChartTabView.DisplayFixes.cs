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
        // Event hooks are installed from the constructor, not a field initializer.
        private bool _displayFixesInstalled;

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
            ChartSettingsManager.SettingsChanged += DisplayFixes_SettingsChanged;
            ApplyDisplayFixes();
        }

        private void DisplayFixes_Unloaded(object sender, RoutedEventArgs e)
        {
            Chart.PreviewMouseMove -= DisplayFixes_MouseMove;
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

                if (_bars.Any(x => x.Timestamp.HasValue && x.Timestamp.Value > DateTime.MinValue))
                    Chart.Plot.Axes.DateTimeTicksBottom();

                ApplyCrosshairSettings(current);
                ApplyFinancialLineWidths(current);
                ApplyGridStyle(current);

                Chart.Refresh();
                if (_volumeVisible)
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
                else if (typeName.Contains("OHLC", StringComparison.OrdinalIgnoreCase))
                {
                    SetNestedLineWidth(plottable, "RisingLineStyle", settings.BarLineWidth);
                    SetNestedLineWidth(plottable, "FallingLineStyle", settings.BarLineWidth);
                    SetNestedLineWidth(plottable, "RisingStyle", settings.BarLineWidth);
                    SetNestedLineWidth(plottable, "FallingStyle", settings.BarLineWidth);
                }
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

                double x = _crosshair.Position.X;
                int index = FindNearestBarIndex(x);
                if (index < 0 || index >= _bars.Count)
                    return;

                MarketBar bar = _bars[index];
                string dateText = GetDisplayDateText(bar, index);

                ChartInfoTextBlock.Text =
                    $"{_symbol.Symbol}   O: {bar.Open:N2}   H: {bar.High:N2}   L: {bar.Low:N2}   C: {bar.Close:N2}   V: {bar.Volume:N0}";

                _crosshair.HorizontalLine.Text = bar.Close.ToString("N2", CultureInfo.InvariantCulture);
                _crosshair.VerticalLine.Text = dateText;
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
    }
}
