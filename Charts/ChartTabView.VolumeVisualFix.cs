using System;
using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _volumeVisualFixRegistered = RegisterVolumeVisualFix();

        private static bool RegisterVolumeVisualFix()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(VolumeVisualFix_Loaded));
            return true;
        }

        private static void VolumeVisualFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart)
                return;

            ChartSettingsManager.SettingsChanged -= chart.VolumeVisualFix_SettingsChanged;
            ChartSettingsManager.SettingsChanged += chart.VolumeVisualFix_SettingsChanged;
            chart.ApplyVolumeVisualFixes();
        }

        private void VolumeVisualFix_SettingsChanged(object? sender, EventArgs e)
        {
            if (!IsLoaded)
                return;

            if (Dispatcher.CheckAccess())
                ApplyVolumeVisualFixes();
            else
                Dispatcher.InvokeAsync(ApplyVolumeVisualFixes);
        }

        private void ApplyVolumeVisualFixes()
        {
            if (!IsLoaded)
                return;

            try
            {
                _settings = ChartSettingsManager.Current;

                const float leftPanel = 85f;
                const float rightPanel = 30f;
                const float bottomPanel = 55f;

                Chart.Plot.Axes.Left.MinimumSize = leftPanel;
                Chart.Plot.Axes.Right.MinimumSize = rightPanel;
                Chart.Plot.Axes.Bottom.MinimumSize = bottomPanel;
                VolumeChart.Plot.Axes.Left.MinimumSize = leftPanel;
                VolumeChart.Plot.Axes.Right.MinimumSize = rightPanel;
                VolumeChart.Plot.Axes.Bottom.MinimumSize = bottomPanel;

                // Settings changes only restyle the existing volume plot. They do not
                // change its axis limits or trigger a second synchronization pass.
                ApplyVolumeBarSettings();
                ApplyVolumeCrosshairSettings();

                Chart.Refresh();
                VolumeChart.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"TradeIt volume visual settings error: {ex}");
            }
        }

        private void ApplyVolumeBarSettings()
        {
            ScottPlot.Color black = ScottPlot.Color.FromHex("#000000");
            double width = Math.Max(0.05, _settings.VolumeBarWidth);

            foreach (var plottable in VolumeChart.Plot.GetPlottables())
            {
                if (plottable is not ScottPlot.Plottables.BarPlot barPlot)
                    continue;

                // ScottPlot 5 BarPlot has no BarWidth property. The width belongs to
                // each ScottPlot.Bar, while Color applies a common fill color.
                barPlot.Color = black;

                foreach (ScottPlot.Bar bar in barPlot.Bars)
                {
                    bar.Size = width;
                    bar.FillColor = black;
                    bar.LineColor = black;
                    bar.LineWidth = (float)Math.Max(0.5, width);
                }
            }
        }
    }
}
