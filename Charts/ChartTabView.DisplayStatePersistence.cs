using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _displayStatePersistenceRegistered = RegisterDisplayStatePersistence();
        private bool _displayStatePersistenceInitialized;

        private static bool RegisterDisplayStatePersistence()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(DisplayStatePersistence_Loaded));
            return true;
        }

        private static void DisplayStatePersistence_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart || chart._displayStatePersistenceInitialized)
                return;

            chart._displayStatePersistenceInitialized = true;

            ChartSettings settings = ChartSettingsManager.Current;
            chart._gridVisible = settings.GridVisible;
            chart._crosshairVisible = settings.CrosshairVisible;

            chart.ApplyGridDisplayState();
            chart.ApplyCrosshairDisplayState();
            chart.SetVolumeVisible(settings.VolumeVisible, false);

            chart.CrosshairButton.Content = chart._crosshairVisible
                ? "Crosshair روشن"
                : "Crosshair خاموش";
            chart.VolumeButton.Content = chart._volumeVisible
                ? "پنهان کردن حجم"
                : "نمایش حجم";
            chart.GridButton.Content = chart._gridVisible
                ? "GRID"
                : "GRID خاموش";

            chart.Chart.AddHandler(
                UIElement.PreviewMouseDownEvent,
                new MouseButtonEventHandler(chart.DisplayStatePersistence_ChartMouseDown),
                true);
            chart.VolumeChart.AddHandler(
                UIElement.PreviewMouseDownEvent,
                new MouseButtonEventHandler(chart.DisplayStatePersistence_ChartMouseDown),
                true);

            chart.CrosshairButton.Click += chart.DisplayStatePersistence_ButtonClick;
            chart.VolumeButton.Click += chart.DisplayStatePersistence_ButtonClick;
            chart.GridButton.Click += chart.DisplayStatePersistence_ButtonClick;
        }

        private void DisplayStatePersistence_ButtonClick(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(
                DispatcherPriority.Input,
                new Action(SaveCurrentDisplayState));
        }

        private void DisplayStatePersistence_ChartMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Middle)
                return;

            Dispatcher.BeginInvoke(
                DispatcherPriority.Input,
                new Action(SaveCurrentDisplayState));
        }

        private void SaveCurrentDisplayState()
        {
            try
            {
                ChartSettings settings = ChartSettingsManager.Current;
                settings.GridVisible = _gridVisible;
                settings.CrosshairVisible = _crosshairVisible;
                settings.VolumeVisible = _volumeVisible;
                ChartSettingsManager.Save(settings);
            }
            catch
            {
                // Display-state persistence must never prevent chart interaction.
            }
        }

        private void ApplyGridDisplayState()
        {
            SetGridVisibility(Chart, _gridVisible);
            SetGridVisibility(VolumeChart, _gridVisible);
        }

        private void ApplyCrosshairDisplayState()
        {
            if (_crosshair != null)
                _crosshair.IsVisible = _crosshairVisible && _chartVisible && _crosshairMouseInside;
        }
    }
}
