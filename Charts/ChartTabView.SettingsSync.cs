using System;
using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _settingsSyncHandlerRegistered = RegisterSettingsSyncHandler();

        private static bool RegisterSettingsSyncHandler()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent, new RoutedEventHandler(ChartTabView_Loaded));
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.UnloadedEvent, new RoutedEventHandler(ChartTabView_Unloaded));
            return true;
        }

        private static void ChartTabView_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart) return;
            ChartSettingsManager.SettingsChanged -= chart.ChartSettingsManager_SettingsChanged;
            ChartSettingsManager.SettingsChanged += chart.ChartSettingsManager_SettingsChanged;
            chart.ApplyStoredChartSettings();
        }

        private static void ChartTabView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                ChartSettingsManager.SettingsChanged -= chart.ChartSettingsManager_SettingsChanged;
        }

        private void ChartSettingsManager_SettingsChanged(object? sender, EventArgs e)
        {
            if (Dispatcher.CheckAccess()) ApplyStoredChartSettings();
            else Dispatcher.InvokeAsync(ApplyStoredChartSettings);
        }

        private void ApplyStoredChartSettings()
        {
            try
            {
                _settings = ChartSettingsManager.Current;
                _gridVisible = _settings.GridVisible;
                ApplyGridStyle(Chart);
                ApplyGridStyle(VolumeChart);
                ApplyCrosshairStyle();

                Chart.Plot.FigureBackground.Color = ScottPlot.Color.FromHtml(_settings.FigureBackground);
                Chart.Plot.DataBackground.Color = ScottPlot.Color.FromHtml(_settings.DataBackground);
                Chart.Plot.Axes.Color(ScottPlot.Color.FromHtml(_settings.AxisColor));
                VolumeChart.Plot.FigureBackground.Color = ScottPlot.Color.FromHtml(_settings.FigureBackground);
                VolumeChart.Plot.DataBackground.Color = ScottPlot.Color.FromHtml(_settings.DataBackground);
                VolumeChart.Plot.Axes.Color(ScottPlot.Color.FromHtml(_settings.AxisColor));
                Chart.Refresh();
                VolumeChart.Refresh();
            }
            catch { }
        }

        private void ApplyGridStyle(ScottPlot.WPF.WpfPlot plot)
        {
            plot.Plot.Grid.IsVisible = _settings.GridVisible;
            plot.Plot.Grid.LineColor = ScottPlot.Color.FromHtml(_settings.GridColor);
            plot.Plot.Grid.LinePattern = ParseLinePattern(_settings.GridPattern);
            plot.Plot.Grid.MajorLineWidth = (float)_settings.GridLineWidth;
            plot.Plot.Grid.MinorLineWidth = (float)_settings.GridLineWidth;
        }

        private void ApplyCrosshairStyle()
        {
            if (_crosshair == null) return;
            _crosshair.LineColor = ScottPlot.Color.FromHtml(_settings.CrosshairColor);
            _crosshair.LineWidth = (float)_settings.CrosshairLineWidth;
            _crosshair.LinePattern = ParseLinePattern(_settings.CrosshairPattern);
        }

        private static ScottPlot.LinePattern ParseLinePattern(string? value)
        {
            return value switch
            {
                "Dotted" => ScottPlot.LinePattern.Dotted,
                "Dashed" => ScottPlot.LinePattern.Dashed,
                "DenselyDashed" => ScottPlot.LinePattern.DenselyDashed,
                _ => ScottPlot.LinePattern.Solid
            };
        }
    }
}
