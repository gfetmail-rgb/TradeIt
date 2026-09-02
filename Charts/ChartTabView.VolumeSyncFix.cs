using System;
using System.Windows;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _volumeSyncFixRegistered = RegisterVolumeSyncFix();
        private bool _volumeSyncFixLoaded;
        private bool _volumeSyncRefreshPending;

        private static bool RegisterVolumeSyncFix()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(VolumeSyncFix_Loaded));
            return true;
        }

        private static void VolumeSyncFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart || chart._volumeSyncFixLoaded)
                return;

            chart._volumeSyncFixLoaded = true;

            // Volume is a follower only. It must never pan/zoom by itself.
            chart.VolumeChart.UserInputProcessor.IsEnabled = false;

            // Recalculate volume Y immediately after every main-chart interaction.
            chart.Chart.PreviewMouseMove += chart.VolumeSyncFix_MainChartMouseMove;
            chart.Chart.PreviewMouseLeftButtonUp += chart.VolumeSyncFix_MainChartInteractionFinished;
            chart.Chart.PreviewMouseRightButtonUp += chart.VolumeSyncFix_MainChartInteractionFinished;
            chart.Chart.PreviewMouseWheel += chart.VolumeSyncFix_MainChartWheelFinished;

            foreach (System.Windows.Controls.Button button in
                     chart.FindVisualChildren<System.Windows.Controls.Button>())
            {
                string content = button.Content?.ToString() ?? string.Empty;
                if (content == "Zoom +" || content == "Zoom -" || content == "Reset Zoom")
                    button.PreviewMouseLeftButtonUp += chart.VolumeSyncFix_MainChartInteractionFinished;
            }

            chart.Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(chart.RefreshVolumeFollower));
        }

        private void VolumeSyncFix_MainChartMouseMove(object? sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_volumeVisible)
                return;

            if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed &&
                e.RightButton != System.Windows.Input.MouseButtonState.Pressed)
                return;

            ScheduleVolumeFollowerRefresh();
        }

        private void VolumeSyncFix_MainChartInteractionFinished(object? sender, RoutedEventArgs e)
        {
            ScheduleVolumeFollowerRefresh();
        }

        private void VolumeSyncFix_MainChartWheelFinished(object? sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            ScheduleVolumeFollowerRefresh();
        }

        private void ScheduleVolumeFollowerRefresh()
        {
            if (!_volumeVisible || _volumeSyncRefreshPending)
                return;

            _volumeSyncRefreshPending = true;

            Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() =>
                {
                    _volumeSyncRefreshPending = false;
                    RefreshVolumeFollower();
                }));
        }

        private void RefreshVolumeFollower()
        {
            if (!_volumeVisible || _bars.Count == 0)
                return;

            var mainLimits = Chart.Plot.Axes.GetLimits();
            double left = mainLimits.Left;
            double right = mainLimits.Right;

            if (!double.IsFinite(left) || !double.IsFinite(right) || right <= left)
                return;

            double maxVisibleVolume = 0;

            for (int i = 0; i < _bars.Count; i++)
            {
                double x = GetBarDateTime(_bars[i], i).ToOADate();
                if (x < left || x > right)
                    continue;

                double volume = _bars[i].Volume / VolumeScale;
                if (double.IsFinite(volume) && volume > maxVisibleVolume)
                    maxVisibleVolume = volume;
            }

            // If the viewport falls between bars, use the nearest bar so the
            // volume panel never becomes visually empty.
            if (maxVisibleVolume <= 0)
            {
                int nearest = 0;
                double nearestDistance = double.MaxValue;

                for (int i = 0; i < _bars.Count; i++)
                {
                    double x = GetBarDateTime(_bars[i], i).ToOADate();
                    double distance = x < left ? left - x : x > right ? x - right : 0;

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = i;
                    }
                }

                double volume = _bars[nearest].Volume / VolumeScale;
                if (double.IsFinite(volume) && volume > 0)
                    maxVisibleVolume = volume;
            }

            // The tallest visible bar is intentionally close to the ceiling.
            double volumeTop = maxVisibleVolume > 0
                ? maxVisibleVolume * 1.05
                : 1.0;

            VolumeChart.Plot.Axes.SetLimits(
                left,
                right,
                0,
                volumeTop);

            ConfigureVolumeAxes();
            VolumeChart.Refresh();
        }

        private System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>()
            where T : DependencyObject
        {
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(this);

            for (int i = 0; i < count; i++)
            {
                DependencyObject child =
                    System.Windows.Media.VisualTreeHelper.GetChild(this, i);

                if (child is T typed)
                    yield return typed;

                if (child is FrameworkElement element)
                {
                    foreach (T descendant in FindVisualChildren<T>(element))
                        yield return descendant;
                }
            }
        }

        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject root)
            where T : DependencyObject
        {
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);

            for (int i = 0; i < count; i++)
            {
                DependencyObject child =
                    System.Windows.Media.VisualTreeHelper.GetChild(root, i);

                if (child is T typed)
                    yield return typed;

                foreach (T descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }
    }
}
