using System;
using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _unifiedBordersRegistered = RegisterUnifiedBorders();

        private static bool RegisterUnifiedBorders()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent, new RoutedEventHandler(UnifiedBorders_Loaded));
            return true;
        }

        private static void UnifiedBorders_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart) return;
            chart.ApplyUnifiedPlotBorders();
            ChartSettingsManager.SettingsChanged -= chart.UnifiedBorders_SettingsChanged;
            ChartSettingsManager.SettingsChanged += chart.UnifiedBorders_SettingsChanged;
        }

        private void UnifiedBorders_SettingsChanged(object? sender, EventArgs e)
        {
            if (Dispatcher.CheckAccess()) ApplyUnifiedPlotBorders();
            else Dispatcher.InvokeAsync(ApplyUnifiedPlotBorders);
        }

        private void ApplyUnifiedPlotBorders()
        {
            try
            {
                _settings = ChartSettingsManager.Current;
                ScottPlot.Color borderColor = ScottPlot.Color.FromHtml(_settings.AxisColor);
                Chart.Plot.DataBorder = new ScottPlot.LineStyle { Color = borderColor, Width = 1, Pattern = ScottPlot.LinePattern.Solid };
                Chart.Plot.FigureBorder = new ScottPlot.LineStyle { Color = borderColor, Width = 1, Pattern = ScottPlot.LinePattern.Solid };
                Chart.Plot.Axes.Frame(false);
                Chart.Refresh();
            }
            catch { }
        }
    }
}