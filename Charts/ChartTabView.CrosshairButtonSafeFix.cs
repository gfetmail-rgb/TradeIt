using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _crosshairButtonSafeFixRegistered = RegisterCrosshairButtonSafeFix();
        private bool _crosshairButtonSafeFixAttached;

        private static bool RegisterCrosshairButtonSafeFix()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(CrosshairButtonSafeFix_Loaded));
            return true;
        }

        private static void CrosshairButtonSafeFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.AttachCrosshairButtonSafeFix();
        }

        private void AttachCrosshairButtonSafeFix()
        {
            if (_crosshairButtonSafeFixAttached)
                return;

            _crosshairButtonSafeFixAttached = true;
            CrosshairButton.Click += CrosshairButtonSafeFix_Click;
        }

        private void CrosshairButtonSafeFix_Click(object sender, RoutedEventArgs e)
        {
            // This handler is intentionally independent of the active drawing tool.
            // Do not change _activeDrawingTool or Chart.UserInputProcessor here.
            _crosshairVisible = !_crosshairVisible;

            if (_crosshair != null)
            {
                _crosshair.IsVisible = _crosshairVisible &&
                                       _chartVisible &&
                                       (_crosshairMouseInside || !_hasInitialView);
            }

            CrosshairButton.IsChecked = _crosshairVisible;
            CrosshairButton.Content = _crosshairVisible ? "✚" : "✚";
            SaveCurrentDisplayState();

            // Refresh after the button event has completely finished. This prevents
            // ScottPlot's active drawing/input pipeline from being refreshed from
            // inside the ToggleButton click route.
            Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() =>
                {
                    if (!IsLoaded || Chart == null)
                        return;
                    Chart.Refresh();
                }));

            e.Handled = true;
        }
    }
}
