using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TradeIt.Models;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _volumeSyncFixRegistered =
            RegisterVolumeSyncFix();

        private bool _volumeSyncFixLoaded;

        private static bool RegisterVolumeSyncFix()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(VolumeSyncFix_Loaded));

            return true;
        }

        private static void VolumeSyncFix_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart ||
                chart._volumeSyncFixLoaded)
            {
                return;
            }

            chart._volumeSyncFixLoaded = true;

            // Volume is a follower of the price chart.
            // It must not have its own pan/zoom interaction.
            chart.VolumeChart.UserInputProcessor.IsEnabled = false;

            chart.Chart.PreviewMouseLeftButtonUp +=
                chart.VolumeSyncFix_MainChartInteractionFinished;

            chart.Chart.PreviewMouseRightButtonUp +=
                chart.VolumeSyncFix_MainChartInteractionFinished;

            chart.Chart.PreviewMouseWheel +=
                chart.VolumeSyncFix_MainChartInteractionFinished;

            // Zoom buttons are unnamed in XAML, so locate them by their
            // existing displayed content and refresh after their Click
            // handlers have changed the price-axis limits.
            foreach (Button button in chart.FindVisualChildren<Button>())
            {
                string content = button.Content?.ToString() ?? string.Empty;

                if (content == "Zoom +" ||
                    content == "Zoom -" ||
                    content == "Reset Zoom")
                {
                    button.PreviewMouseLeftButtonUp +=
                        chart.VolumeSyncFix_MainChartInteractionFinished;
                }
            }

            chart.Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(chart.RefreshVolumeFollower));
        }

        private void VolumeSyncFix_MainChartInteractionFinished(
            object? sender,
            RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(RefreshVolumeFollower));
        }

        private void VolumeSyncFix_MainChartInteractionFinished(
            object? sender,
            System.Windows.Input.MouseWheelEventArgs e)
        {
            Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(RefreshVolumeFollower));
        }

        private void RefreshVolumeFollower()
        {
            if (!_volumeVisible ||
                _bars.Count == 0)
            {
                return;
            }

            var mainLimits = Chart.Plot.Axes.GetLimits();

            double left = mainLimits.Left;
            double right = mainLimits.Right;

            if (!double.IsFinite(left) ||
                !double.IsFinite(right) ||
                right <= left)
            {
                return;
            }

            // Recalculate the volume ceiling from ONLY the bars currently
            // visible in the price chart. This is the critical part: the
            // previous implementation kept the old volume ceiling, so after
            // zooming the bars could remain compressed until another redraw.
            double maxVisibleVolume = 0;

            for (int i = 0; i < _bars.Count; i++)
            {
                double x =
                    GetBarDateTime(_bars[i], i).ToOADate();

                if (x < left || x > right)
                    continue;

                double volume =
                    _bars[i].Volume / VolumeScale;

                if (double.IsFinite(volume) && volume > maxVisibleVolume)
                    maxVisibleVolume = volume;
            }

            // If the current range falls between candle timestamps, still
            // use the nearest visible candle so the volume panel never gets
            // an empty/flat scale during a zoom transition.
            if (maxVisibleVolume <= 0)
            {
                int nearest = 0;
                double nearestDistance = double.MaxValue;

                for (int i = 0; i < _bars.Count; i++)
                {
                    double x =
                        GetBarDateTime(_bars[i], i).ToOADate();

                    double distance =
                        x < left ? left - x :
                        x > right ? x - right : 0;

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = i;
                    }
                }

                double volume =
                    _bars[nearest].Volume / VolumeScale;

                if (double.IsFinite(volume) && volume > 0)
                    maxVisibleVolume = volume;
            }

            double volumeTop =
                maxVisibleVolume > 0
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

        private System.Collections.Generic.IEnumerable<T>
            FindVisualChildren<T>() where T : DependencyObject
        {
            int count =
                System.Windows.Media.VisualTreeHelper.GetChildrenCount(this);

            for (int i = 0; i < count; i++)
            {
                DependencyObject child =
                    System.Windows.Media.VisualTreeHelper.GetChild(this, i);

                if (child is T typed)
                    yield return typed;

                if (child is FrameworkElement element)
                {
                    foreach (T descendant in
                        FindVisualChildren<T>(element))
                    {
                        yield return descendant;
                    }
                }
            }
        }

        private static System.Collections.Generic.IEnumerable<T>
            FindVisualChildren<T>(DependencyObject root)
            where T : DependencyObject
        {
            int count =
                System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);

            for (int i = 0; i < count; i++)
            {
                DependencyObject child =
                    System.Windows.Media.VisualTreeHelper.GetChild(root, i);

                if (child is T typed)
                    yield return typed;

                foreach (T descendant in
                    FindVisualChildren<T>(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
