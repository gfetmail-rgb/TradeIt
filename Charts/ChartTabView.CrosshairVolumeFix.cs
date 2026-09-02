using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TradeIt.Models;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private ScottPlot.Plottables.Crosshair? _volumeCrosshair;

        private static readonly bool _crosshairVisualFixRegistered = RegisterCrosshairVisualFix();

        private static bool RegisterCrosshairVisualFix()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                UIElement.PreviewMouseMoveEvent,
                new System.Windows.Input.MouseEventHandler(CrosshairVisualFix_PreviewMouseMove),
                true);

            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                UIElement.MouseLeaveEvent,
                new System.Windows.Input.MouseEventHandler(CrosshairVisualFix_MouseLeave),
                true);

            return true;
        }

        private static void CrosshairVisualFix_PreviewMouseMove(
            object sender,
            System.Windows.Input.MouseEventArgs e)
        {
            if (sender is not ChartTabView view ||
                !view._volumeVisible ||
                !view._chartVisible ||
                !view._crosshairVisible)
            {
                return;
            }

            if (e.OriginalSource is not DependencyObject source)
                return;

            ScottPlot.WPF.WpfPlot? plot = FindPlot(source);

            if (!ReferenceEquals(plot, view.Chart) &&
                !ReferenceEquals(plot, view.VolumeChart))
            {
                return;
            }

            var result = new WpfPointResult(
                e.GetPosition(plot),
                ReferenceEquals(plot, view.Chart));

            view.UpdateSynchronizedCrosshair(result);
        }

        private static void CrosshairVisualFix_MouseLeave(
            object sender,
            System.Windows.Input.MouseEventArgs e)
        {
            if (sender is not ChartTabView view)
                return;

            if (e.OriginalSource is not DependencyObject source)
                return;

            ScottPlot.WPF.WpfPlot? plot = FindPlot(source);

            if (ReferenceEquals(plot, view.Chart) ||
                ReferenceEquals(plot, view.VolumeChart))
            {
                if (view._volumeCrosshair != null)
                    view._volumeCrosshair.IsVisible = false;

                view.VolumeInfoTextBlock.Text = string.Empty;
                view.VolumeChart.Refresh();
            }
        }

        private void UpdateSynchronizedCrosshair(WpfPointResult result)
        {
            ScottPlot.WPF.WpfPlot sourcePlot =
                result.IsMainChart ? Chart : VolumeChart;

            if (!TryGetChartCoordinates(
                    sourcePlot,
                    result.Position,
                    out ScottPlot.Coordinates coordinates))
            {
                return;
            }

            double x = coordinates.X;
            var priceLimits = Chart.Plot.Axes.GetLimits();

            if (x < priceLimits.Left || x > priceLimits.Right)
                return;

            MarketBar? nearestBar = GetNearestBar(x);

            if (nearestBar != null)
            {
                ScottPlot.Color candleColor =
                    nearestBar.Close >= nearestBar.Open
                        ? ScottPlot.Color.FromHtml(_settings.RisingColor)
                        : ScottPlot.Color.FromHtml(_settings.FallingColor);

                ApplyCrosshairColor(candleColor);
            }

            if (_crosshair != null)
            {
                double priceY = result.IsMainChart
                    ? coordinates.Y
                    : (priceLimits.Bottom + priceLimits.Top) / 2.0;

                _crosshair.Position =
                    new ScottPlot.Coordinates(x, priceY);

                _crosshair.IsVisible = true;
            }

            EnsureVolumeCrosshair();

            if (_volumeCrosshair != null)
            {
                _volumeCrosshair.Position =
                    new ScottPlot.Coordinates(x, 0);

                _volumeCrosshair.IsVisible = true;
            }

            if (nearestBar != null)
            {
                double volumeK = nearestBar.Volume / VolumeScale;

                if (!double.IsFinite(volumeK) || volumeK < 0)
                    volumeK = 0;

                VolumeInfoTextBlock.Text =
                    $"حجم: {volumeK:N0} K";
            }

            Chart.Refresh();
            VolumeChart.Refresh();
        }

        private void ApplyCrosshairColor(ScottPlot.Color color)
        {
            if (_crosshair == null)
                return;

            _crosshair.LineColor = color;
            _crosshair.MarkerColor = color;
            _crosshair.MarkerLineColor = color;
            _crosshair.TextBackgroundColor = color;
            _crosshair.TextColor = ScottPlot.Colors.White;

            if (_volumeCrosshair != null)
                _volumeCrosshair.LineColor = color;
        }

        private void EnsureVolumeCrosshair()
        {
            if (_volumeCrosshair != null &&
                VolumeChart.Plot.GetPlottables().Contains(_volumeCrosshair))
            {
                return;
            }

            _volumeCrosshair =
                VolumeChart.Plot.Add.Crosshair(0, 0);

            _volumeCrosshair.HorizontalLine.IsVisible = false;
            _volumeCrosshair.LineWidth = 1;
            _volumeCrosshair.LinePattern = ScottPlot.LinePattern.Dashed;
            _volumeCrosshair.MarkerSize = 0;
            _volumeCrosshair.IsVisible = false;
        }

        private MarketBar? GetNearestBar(double x)
        {
            if (_bars.Count == 0)
                return null;

            int nearestIndex = -1;
            double nearestDistance = double.MaxValue;

            for (int i = 0; i < _bars.Count; i++)
            {
                double barX =
                    GetBarDateTime(_bars[i], i).ToOADate();

                double distance = Math.Abs(barX - x);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = i;
                }
            }

            return nearestIndex >= 0 ? _bars[nearestIndex] : null;
        }

        private static ScottPlot.WPF.WpfPlot? FindPlot(DependencyObject source)
        {
            DependencyObject? current = source;

            while (current != null)
            {
                if (current is ScottPlot.WPF.WpfPlot plot)
                    return plot;

                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private readonly record struct WpfPointResult(
            System.Windows.Point Position,
            bool IsMainChart);
    }
}
