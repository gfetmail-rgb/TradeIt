using System;
using System.Windows;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private void FixChartMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_volumeVisible || !_crosshairVisible || !_chartVisible) return;
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                if (!_volumeVisible || VolumeContainer.Visibility != Visibility.Visible) return;
                SyncVolumeXAxisAndScale();
                if (_crosshair != null && _crosshair.IsVisible)
                {
                    SetCrosshairAtX(_crosshair.Position.X);
                    Chart.Refresh();
                    VolumeChart.Refresh();
                }
            }));
        }

        private void FixVolumeMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_volumeVisible || !_crosshairVisible || !_chartVisible) return;
            EnsureVolumeCrosshair();
            if (!TryGetChartCoordinates(VolumeChart, e.GetPosition(VolumeChart), out ScottPlot.Coordinates p)) return;
            var main = Chart.Plot.Axes.GetLimits();
            if (p.X < main.Left || p.X > main.Right) return;
            int i = NearestBar(p.X);
            if (i < 0) return;
            double x = BarX(i);
            double y = (main.Bottom + main.Top) / 2;
            if (_crosshair != null)
            {
                _crosshair.Position = new ScottPlot.Coordinates(x, y);
                _crosshair.IsVisible = true;
            }
            SetCrosshairAtX(x);
            UpdateMouseInformation(new ScottPlot.Coordinates(x, y));
            Chart.Refresh();
            VolumeChart.Refresh();
        }

        private void FixVolumeMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_volumeCrosshair != null)
            {
                _volumeCrosshair.IsVisible = false;
                VolumeChart.Refresh();
            }
        }
    }
}