using System;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _displayFixesRegistered = RegisterDisplayFixes();

        private static bool RegisterDisplayFixes()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent, new RoutedEventHandler(DisplayFixes_Loaded));
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

        private void ApplyDisplayFixesNow()
        {
            try
            {
                _settings = ChartSettingsManager.Current;
                _gridVisible = _settings.GridVisible;
                ApplyDisplayCrosshairSettings();
                ApplyDisplayCrosshairLayout();
                ApplyDisplayGridSettings(Chart);
                ApplyDisplayBackgroundAndAxes(Chart);
                ApplyDisplaySeriesWidths();
                ConfigureDisplayDateAxis(Chart);
                ApplyUnifiedPlotBorders();
                Chart.Refresh();
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
            float leftWidth = Math.Max(85, Chart.Plot.Axes.Left.MinimumSize);
            float rightWidth = Math.Max(0, Chart.Plot.Axes.Right.MinimumSize);
            Chart.Plot.Axes.Left.MinimumSize = leftWidth;
            Chart.Plot.Axes.Right.MinimumSize = rightWidth;
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
                if (hasRealTimestamp)
                {
                    var axis = plot.Plot.Axes.DateTimeTicksBottom();
                    if (axis.TickGenerator is ScottPlot.TickGenerators.DateTimeAutomatic auto)
                        auto.LabelFormatter = dt => dt.TimeOfDay == TimeSpan.Zero ? dt.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture) : dt.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);
                }
                else
                {
                    var axis = plot.Plot.Axes.NumericTicksBottom();
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
                    double lastPosition = GetBarDateTime(_bars[count - 1], count - 1).ToOADate();
                    if (positions.Count == 0 || Math.Abs(positions[^1] - lastPosition) > 1e-12)
                    {
                        positions.Add(lastPosition);
                        labels.Add($"کندل {count}");
                    }
                    axis.TickGenerator = new ScottPlot.TickGenerators.NumericManual(positions.ToArray(), labels.ToArray());
                }
            }
            catch { }
        }
    }
}