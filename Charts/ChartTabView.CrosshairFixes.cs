using System;
using System.Windows;
using System.Windows.Input;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        /// <summary>
        /// Applies the stored chart settings before the first Loaded event and
        /// installs a second mouse-move handler which runs after the legacy
        /// handler in ChartTabView.xaml.cs. The second handler snaps X to the
        /// nearest actual bar, so the crosshair never rests between candles.
        /// </summary>
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            _settings = ChartSettingsManager.Current;
            _gridVisible = _settings.GridVisible;

            ApplyGridStyle(Chart);
            ApplyGridStyle(VolumeChart);
            ApplyCrosshairStyle();

            Chart.PreviewMouseMove += CrosshairFixes_PreviewMouseMove;
            VolumeChart.PreviewMouseMove += CrosshairFixes_VolumeMouseMove;
        }

        private void CrosshairFixes_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_crosshair == null || !_chartVisible || !_crosshairVisible || _bars.Count == 0)
                return;

            Point mouse = e.GetPosition(Chart);
            if (!TryGetChartCoordinates(Chart, mouse, out ScottPlot.Coordinates coordinates))
                return;

            int nearestIndex = FindNearestBarIndex(coordinates.X);
            if (nearestIndex < 0)
                return;

            double snappedX = GetBarDateTime(_bars[nearestIndex], nearestIndex).ToOADate();

            _crosshair.Position = new ScottPlot.Coordinates(snappedX, coordinates.Y);
            _crosshair.HorizontalLine.Text = coordinates.Y.ToString("N2");
            _crosshair.HorizontalLine.LabelOppositeAxis = false;
            _crosshair.IsVisible = true;
            _crosshairMouseInside = true;

            UpdateCrosshairBarInformation(nearestIndex, coordinates.Y);
            Chart.Refresh();
        }

        private void CrosshairFixes_VolumeMouseMove(object sender, MouseEventArgs e)
        {
            if (_crosshair == null || !_chartVisible || !_crosshairVisible || _bars.Count == 0)
                return;

            Point mouse = e.GetPosition(VolumeChart);
            if (!TryGetChartCoordinates(VolumeChart, mouse, out ScottPlot.Coordinates coordinates))
                return;

            var mainLimits = Chart.Plot.Axes.GetLimits();
            if (coordinates.X < mainLimits.Left || coordinates.X > mainLimits.Right)
                return;

            int nearestIndex = FindNearestBarIndex(coordinates.X);
            if (nearestIndex < 0)
                return;

            double snappedX = GetBarDateTime(_bars[nearestIndex], nearestIndex).ToOADate();
            double y = (mainLimits.Bottom + mainLimits.Top) / 2.0;

            _crosshair.Position = new ScottPlot.Coordinates(snappedX, y);
            _crosshair.HorizontalLine.Text = y.ToString("N2");
            _crosshair.IsVisible = true;
            _crosshairMouseInside = true;

            UpdateCrosshairBarInformation(nearestIndex, y);
            Chart.Refresh();
        }

        private int FindNearestBarIndex(double x)
        {
            if (_bars.Count == 0)
                return -1;

            int nearest = 0;
            double bestDistance = double.MaxValue;

            for (int i = 0; i < _bars.Count; i++)
            {
                double barX = GetBarDateTime(_bars[i], i).ToOADate();
                double distance = Math.Abs(barX - x);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = i;
                }
            }

            return nearest;
        }

        private void UpdateCrosshairBarInformation(int index, double crossPrice)
        {
            if (index < 0 || index >= _bars.Count)
                return;

            MarketBar bar = _bars[index];
            DateTime timestamp = GetBarDateTime(bar, index);

            string dateText;
            if (timestamp.TimeOfDay == TimeSpan.Zero)
                dateText = timestamp.ToString("yyyy/MM/dd");
            else
                dateText = timestamp.ToString("yyyy/MM/dd HH:mm:ss");

            ChartInfoTextBlock.Text =
                $"{_symbol.Symbol} | O: {bar.Open:N2}  H: {bar.High:N2}  L: {bar.Low:N2}  C: {bar.Close:N2}  V: {bar.Volume:N0} | {dateText}";
        }
    }
}
