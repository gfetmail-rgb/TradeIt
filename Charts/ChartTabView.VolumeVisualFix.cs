using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _volumeVisualFixRegistered = RegisterVolumeVisualFix();

        private static bool RegisterVolumeVisualFix()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent, new RoutedEventHandler(VolumeVisualFix_Loaded));
            return true;
        }

        private static void VolumeVisualFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart) return;
            chart.VolumeChart.AddHandler(UIElement.PreviewMouseMoveEvent, new System.Windows.Input.MouseEventHandler(chart.VolumeVisualFix_MouseMove), true);
            ChartSettingsManager.SettingsChanged -= chart.VolumeVisualFix_SettingsChanged;
            ChartSettingsManager.SettingsChanged += chart.VolumeVisualFix_SettingsChanged;
            chart.ApplyVolumeVisualFixes();
        }

        private void VolumeVisualFix_SettingsChanged(object? sender, EventArgs e)
        {
            if (Dispatcher.CheckAccess()) ApplyVolumeVisualFixes();
            else Dispatcher.InvokeAsync(ApplyVolumeVisualFixes);
        }

        private void VolumeVisualFix_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_volumeVisible || !_crosshairVisible || !_chartVisible) return;
            VolumeSync_MouseMove(sender, e);
        }

        private void ApplyVolumeVisualFixes()
        {
            try
            {
                const double leftPanel = 85;
                const double rightPanel = 30;
                const double bottomPanel = 55;

                Chart.Plot.Axes.Left.MinimumSize = leftPanel;
                Chart.Plot.Axes.Right.MinimumSize = rightPanel;
                Chart.Plot.Axes.Bottom.MinimumSize = bottomPanel;
                VolumeChart.Plot.Axes.Left.MinimumSize = leftPanel;
                VolumeChart.Plot.Axes.Right.MinimumSize = rightPanel;
                VolumeChart.Plot.Axes.Bottom.MinimumSize = bottomPanel;

                if (_volumeVisible)
                {
                    SyncVolumeFromPriceLimits();
                    ApplyVolumeBarSettings();
                }

                Chart.Refresh();
                VolumeChart.Refresh();
            }
            catch { }
        }

        private void ApplyVolumeBarSettings()
        {
            string color = _settings?.VolumeColor ?? "#607D8B";
            double width = Math.Max(0.05, _settings?.VolumeBarWidth ?? 0.8);
            object? colorObject = TryCreateScottPlotColor(color);

            foreach (var plottable in VolumeChart.Plot.GetPlottables())
            {
                if (!plottable.GetType().Name.Contains("Bar", StringComparison.OrdinalIgnoreCase)) continue;
                SetPropertyIfPresent(plottable, "BarWidth", width);
                SetPropertyIfPresent(plottable, "Width", width);
                SetPropertyIfPresent(plottable, "LineWidth", (float)width);
                if (colorObject != null)
                {
                    SetPropertyIfPresent(plottable, "Color", colorObject);
                    SetPropertyIfPresent(plottable, "FillColor", colorObject);
                    SetPropertyIfPresent(plottable, "LineColor", colorObject);
                }
            }
        }

        private static void SetPropertyIfPresent(object target, string propertyName, object value)
        {
            try
            {
                PropertyInfo? property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                if (property?.CanWrite != true) return;
                if (property.PropertyType.IsInstanceOfType(value)) { property.SetValue(target, value); return; }
                if (value is double d && property.PropertyType == typeof(float)) property.SetValue(target, (float)d);
                else if (value is float f && property.PropertyType == typeof(double)) property.SetValue(target, (double)f);
            }
            catch { }
        }

        private static object? TryCreateScottPlotColor(string hex)
        {
            try
            {
                Type? colorType = Type.GetType("ScottPlot.Color, ScottPlot");
                if (colorType == null) return null;
                MethodInfo? method = colorType.GetMethod("FromHex", BindingFlags.Public | BindingFlags.Static);
                return method?.Invoke(null, new object[] { hex });
            }
            catch { return null; }
        }
    }
}