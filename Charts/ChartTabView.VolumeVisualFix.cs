using System;
using System.Linq;
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

            chart.VolumeChart.AddHandler(
                UIElement.PreviewMouseMoveEvent,
                new MouseEventHandler(chart.VolumeVisualFix_MouseMove),
                true);

            chart.ApplyVolumeVisualFixes();
        }

        private void VolumeVisualFix_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_volumeVisible || !_crosshairVisible || !_chartVisible)
                return;

            // This handler intentionally receives already-handled mouse events too.
            // The normal VolumeSync handler remains the single source of candle snapping.
            VolumeSync_MouseMove(sender, e);
        }

        private void ApplyVolumeVisualFixes()
        {
            try
            {
                // The data area of both ScottPlot instances must reserve the same
                // horizontal axis-panel widths. Otherwise different Y-label widths
                // make the volume plot visibly wider/narrower than price.
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
            catch
            {
                // ScottPlot minor-version differences must not prevent the chart
                // from being displayed.
            }
        }

        private void ApplyVolumeBarSettings()
        {
            string color = _settings?.VolumeColor ?? "#607D8B";
            double width = Math.Max(0.05, _settings?.VolumeBarWidth ?? 0.8);
            object? colorObject = TryCreateScottPlotColor(color);

            foreach (var plottable in VolumeChart.Plot.GetPlottables())
            {
                Type type = plottable.GetType();
                if (!type.Name.Contains("Bar", StringComparison.OrdinalIgnoreCase))
                    continue;

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
                PropertyInfo? property = target.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Instance | BindingFlags.Public);
                if (property?.CanWrite != true)
                    return;

                if (property.PropertyType.IsInstanceOfType(value))
                {
                    property.SetValue(target, value);
                    return;
                }

                if (value is double d && property.PropertyType == typeof(float))
                    property.SetValue(target, (float)d);
                else if (value is float f && property.PropertyType == typeof(double))
                    property.SetValue(target, (double)f);
            }
            catch { }
        }

        private static object? TryCreateScottPlotColor(string hex)
        {
            try
            {
                MethodInfo? method = typeof(ScottPlot.Color).GetMethod(
                    "FromHtml",
                    BindingFlags.Public | BindingFlags.Static,
                    binder: null,
                    new[] { typeof(string) },
                    modifiers: null);
                return method?.Invoke(null, new object[] { hex });
            }
            catch
            {
                return null;
            }
        }
    }
}