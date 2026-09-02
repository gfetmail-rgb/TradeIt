using System;
using System.Windows;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _volumePanSyncRegistered = RegisterVolumePanSync();
        private bool _volumePanSyncPending;

        private static bool RegisterVolumePanSync()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                UIElement.PreviewMouseMoveEvent,
                new System.Windows.Input.MouseEventHandler(VolumePanSync_PreviewMouseMove),
                true);

            return true;
        }

        private static void VolumePanSync_PreviewMouseMove(
            object sender,
            System.Windows.Input.MouseEventArgs e)
        {
            if (sender is not ChartTabView chart ||
                !chart._volumeVisible ||
                e.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
            {
                return;
            }

            if (e.OriginalSource is not DependencyObject source)
                return;

            ScottPlot.WPF.WpfPlot? plot = FindVolumePanPlot(source);
            if (!ReferenceEquals(plot, chart.Chart))
                return;

            chart.ScheduleVolumePanSync();
        }

        private void ScheduleVolumePanSync()
        {
            if (_volumePanSyncPending)
                return;

            _volumePanSyncPending = true;

            Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() =>
                {
                    _volumePanSyncPending = false;
                    SyncVolumeLimitsToPrice();
                    VolumeChart.Refresh();
                }));
        }

        private static ScottPlot.WPF.WpfPlot? FindVolumePanPlot(DependencyObject source)
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
    }
}
