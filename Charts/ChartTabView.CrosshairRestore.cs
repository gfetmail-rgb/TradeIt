using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _crosshairRestoreRegistered = RegisterCrosshairRestore();
        private bool _crosshairRestoreMouseHooked;

        private static bool RegisterCrosshairRestore()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(CrosshairRestore_Loaded));
            return true;
        }

        private static void CrosshairRestore_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart)
                return;

            // Run after the existing chart/date/time-gap initialization passes.
            // This is deliberately a final reconciliation pass: continuous X
            // coordinates must never be mixed with real DateTime X coordinates.
            chart.Dispatcher.BeginInvoke(
                new Action(chart.RestoreCrosshairAndDateAxis),
                DispatcherPriority.SystemIdle);
        }

        private void RestoreCrosshairAndDateAxis()
        {
            if (_bars.Count == 0)
                return;

            try
            {
                if (_continuousTimeAxisApplied)
                    ConfigureContinuousDateAxis();
                else
                    ConfigureFinalDateAxis();

                if (!_crosshairRestoreMouseHooked)
                {
                    _crosshairRestoreMouseHooked = true;
                    Chart.PreviewMouseMove += CrosshairRestore_MouseMove;
                }

                if (_crosshair == null || !_chartVisible || !_crosshairVisible)
                {
                    if (_crosshair != null)
                        _crosshair.IsVisible = false;
                    Chart.Refresh();
                    return;
                }

                int index = _bars.Count - 1;
                double x = _continuousTimeAxisApplied
                    ? ContinuousX(index)
                    : GetBarDateTime(_bars[index], index).ToOADate();
                double y = _bars[index].Close;

                _crosshair.Position = new ScottPlot.Coordinates(x, y);
                _crosshair.HorizontalLine.Text = y.ToString("N2");
                _crosshair.VerticalLine.Text = GetCrosshairXLabel(index);
                _crosshairMouseInside = true;
                _crosshair.IsVisible = true;

                Chart.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Crosshair/date-axis restore failed: {ex}");
            }
        }

        private void CrosshairRestore_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_continuousTimeAxisApplied || _crosshair == null ||
                !_crosshairVisible || !_chartVisible)
                return;

            try
            {
                ApplyContinuousCrosshair(e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Continuous crosshair update failed: {ex}");
            }
        }
    }
}
