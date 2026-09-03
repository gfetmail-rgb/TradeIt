using System;
using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private void SubscribeToSettingsChanges()
        {
            ChartSettingsManager.SettingsChanged -= ChartSettingsManager_SettingsChanged;
            ChartSettingsManager.SettingsChanged += ChartSettingsManager_SettingsChanged;
        }

        private void ChartSettingsManager_SettingsChanged(object? sender, EventArgs e)
        {
            if (!IsLoaded) return;

            if (Dispatcher.CheckAccess())
                ApplyStoredChartSettings();
            else
                Dispatcher.InvokeAsync(ApplyStoredChartSettings);
        }

        private void ApplyStoredChartSettings()
        {
            void Apply()
            {
                _settings = ChartSettingsManager.Current;
                _gridVisible = _settings.GridVisible;
                _crosshairVisible = _settings.CrosshairVisible;
                _chartType = _settings.ChartType?.Trim().ToLowerInvariant() switch
                {
                    "line" => ChartDisplayType.Line,
                    "bar" => ChartDisplayType.Bar,
                    _ => ChartDisplayType.Candlestick
                };

                int desiredIndex = _chartType switch
                {
                    ChartDisplayType.Line => 1,
                    ChartDisplayType.Bar => 2,
                    _ => 0
                };
                if (ChartTypeComboBox.SelectedIndex != desiredIndex)
                    ChartTypeComboBox.SelectedIndex = desiredIndex;

                // Rebuild the current series from the new settings and preserve
                // the user's current zoom/pan limits.
                if (_bars.Count > 0)
                    DrawChart();
                else
                    ApplyChartVisualSettingsOnly();

                ApplyGridStyle(Chart);
                ApplyCrosshairStyle();
                ApplyGridDisplayState();
                ApplyCrosshairDisplayState();

                GridButton.Content = _gridVisible ? "GRID" : "GRID خاموش";
                CrosshairButton.Content = _crosshairVisible ? "Crosshair روشن" : "Crosshair خاموش";
                Chart.Refresh();
            }

            if (Dispatcher.CheckAccess())
            {
                try { Apply(); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Chart settings apply failed: {ex}");
                }
            }
            else
            {
                Dispatcher.InvokeAsync(() =>
                {
                    try { Apply(); }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Chart settings apply failed: {ex}");
                    }
                });
            }
        }

        private void ApplyChartVisualSettingsOnly()
        {
            Chart.Plot.FigureBackground.Color = ScottPlot.Color.FromHtml(_settings.FigureBackground);
            Chart.Plot.DataBackground.Color = ScottPlot.Color.FromHtml(_settings.DataBackground);
            Chart.Plot.Axes.Color(ScottPlot.Color.FromHtml(_settings.AxisColor));
        }

        private void ApplyGridStyle(ScottPlot.WPF.WpfPlot plot)
        {
            plot.Plot.Grid.IsVisible = _settings.GridVisible;
            plot.Plot.Grid.LineColor = ScottPlot.Color.FromHtml(_settings.GridColor);
            plot.Plot.Grid.LinePattern = ParseSettingsLinePattern(_settings.GridPattern);
            plot.Plot.Grid.MajorLineWidth = (float)Math.Max(0.01, _settings.GridLineWidth);
            plot.Plot.Grid.MinorLineWidth = (float)Math.Max(0.01, _settings.GridLineWidth);
        }

        private void ApplyCrosshairStyle()
        {
            if (_crosshair == null) return;

            _crosshair.LineColor = ScottPlot.Color.FromHtml(_settings.CrosshairColor);
            _crosshair.LineWidth = (float)Math.Max(0.01, _settings.CrosshairLineWidth);
            _crosshair.LinePattern = ParseSettingsLinePattern(_settings.CrosshairPattern);
        }

        private static ScottPlot.LinePattern ParseSettingsLinePattern(string? value) =>
            value?.Trim().ToLowerInvariant() switch
            {
                "dotted" => ScottPlot.LinePattern.Dotted,
                "dashed" => ScottPlot.LinePattern.Dashed,
                "denselydashed" => ScottPlot.LinePattern.DenselyDashed,
                _ => ScottPlot.LinePattern.Solid
            };
    }
}
