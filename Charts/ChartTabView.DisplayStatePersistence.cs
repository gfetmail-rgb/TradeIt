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
            if (sender is not ChartTabView chart)
                return;

            chart.InitializeDisplayStatePersistence();
        }

        // Apply the latest global display state immediately when a chart is created.
        // Loaded is kept as a safety net for charts created before their visual tree
        // is connected, but initialization is performed only once per chart.
        private void InitializeDisplayStatePersistence()
        {
            if (_displayStatePersistenceInitialized)
                return;

            _displayStatePersistenceInitialized = true;

            ChartSettings settings = ChartSettingsManager.Current;
            _settings = settings;
            _gridVisible = settings.GridVisible;
            _crosshairVisible = settings.CrosshairVisible;

            ApplyGridDisplayState();
            ApplyCrosshairDisplayState();
            SetVolumeVisible(settings.VolumeVisible, false);

            CrosshairButton.Content = _crosshairVisible
                ? "Crosshair روشن"
                : "Crosshair خاموش";
            VolumeButton.Content = _volumeVisible
                ? "پنهان کردن حجم"
                : "نمایش حجم";
            GridButton.Content = _gridVisible
                ? "GRID"
                : "GRID خاموش";

            Chart.AddHandler(
                UIElement.PreviewMouseDownEvent,
                new MouseButtonEventHandler(DisplayStatePersistence_ChartMouseDown),
                true);
            VolumeChart.AddHandler(
                UIElement.PreviewMouseDownEvent,
                new MouseButtonEventHandler(DisplayStatePersistence_ChartMouseDown),
                true);

            CrosshairButton.Click += DisplayStatePersistence_ButtonClick;
            VolumeButton.Click += DisplayStatePersistence_ButtonClick;
            GridButton.Click += DisplayStatePersistence_ButtonClick;
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
