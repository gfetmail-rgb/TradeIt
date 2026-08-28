using System;
using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _settingsSyncHandlerRegistered = RegisterSettingsSyncHandler();

        private static bool RegisterSettingsSyncHandler()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(ChartTabView_Loaded));
            return true;
        }

        private static void ChartTabView_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart)
                return;

            ChartSettingsManager.SettingsChanged -= chart.ChartSettingsManager_SettingsChanged;
            ChartSettingsManager.SettingsChanged += chart.ChartSettingsManager_SettingsChanged;
            chart.ApplyStoredChartSettings();
        }

        private void ChartSettingsManager_SettingsChanged(object? sender, EventArgs e)
        {
            _settings = ChartSettingsManager.Current;
            ApplyStoredChartSettings();
        }

        private void ApplyStoredChartSettings()
        {
            _settings = ChartSettingsManager.Current;
            _gridVisible = _settings.GridVisible;

            ApplyGridStyle(Chart);
            ApplyGridStyle(VolumeChart);
            ApplyCrosshairStyle();

            Chart.Refresh();
            if (_volumeVisible)
                VolumeChart.Refresh();
        }

        private void ApplyGridStyle(ScottPlot.WPF.WpfPlot plot)
        {
            plot.Plot.Grid.IsVisible = _gridVisible;
            plot.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHtml(_settings.GridColor);

            var pattern = ParseLinePattern(_settings.GridPattern);
            plot.Plot.Grid.XAxisStyle.MajorLineStyle.Pattern = pattern;
            plot.Plot.Grid.YAxisStyle.MajorLineStyle.Pattern = pattern;
            plot.Plot.Grid.XAxisStyle.MajorLineStyle.Width = (float)_settings.GridLineWidth;
            plot.Plot.Grid.YAxisStyle.MajorLineStyle.Width = (float)_settings.GridLineWidth;
        }

        private void ApplyCrosshairStyle()
        {
            if (_crosshair == null)
                return;

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
