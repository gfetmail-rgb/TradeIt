using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TradeIt.Charts;

namespace TradeIt
{
    public partial class MainWindow
    {
        private Window? _chartFullScreenWindow;
        private ChartTabView? _chartFullScreenView;

        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (_chartFullScreenWindow != null)
            {
                _chartFullScreenWindow.Activate();
                return;
            }

            if (ChartTabs.SelectedItem is not TabItem tab ||
                tab.Content is not ChartTabView sourceChart)
            {
                WpfMessageBox.Show(
                    "ابتدا یک چارت را باز و انتخاب کنید.",
                    "تمام صفحه",
                    WpfMessageBoxButton.OK,
                    WpfMessageBoxImage.Information);
                return;
            }

            EnterChartFullScreen(sourceChart);
        }

        private void FullScreenExitButton_Click(object sender, RoutedEventArgs e)
        {
            ExitChartFullScreen();
        }

        private void EnterChartFullScreen(ChartTabView sourceChart)
        {
            if (_chartFullScreenWindow != null)
                return;

            try
            {
                ChartTabView fullScreenChart = sourceChart.CreateFullScreenClone();
                _chartFullScreenView = fullScreenChart;
                _isFullScreen = true;

                var host = new Grid
                {
                    Background = Brushes.White
                };
                host.Children.Add(fullScreenChart);

                var window = new Window
                {
                    Title = "TradeIt - نمودار",
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    WindowState = WindowState.Maximized,
                    WindowStyle = WindowStyle.None,
                    ResizeMode = ResizeMode.NoResize,
                    ShowInTaskbar = true,
                    Background = Brushes.White,
                    Content = host
                };

                window.KeyDown += FullScreenWindow_KeyDown;
                window.Closed += FullScreenWindow_Closed;

                _chartFullScreenWindow = window;
                window.Show();
                window.Activate();
            }
            catch (Exception ex)
            {
                _chartFullScreenWindow = null;
                _chartFullScreenView = null;
                _isFullScreen = false;

                WpfMessageBox.Show(
                    $"خطا در باز کردن نمودار تمام صفحه:\n{ex.Message}",
                    "تمام صفحه",
                    WpfMessageBoxButton.OK,
                    WpfMessageBoxImage.Error);
            }
        }

        private void FullScreenWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ExitChartFullScreen();
                e.Handled = true;
            }
        }

        private void FullScreenWindow_Closed(object? sender, EventArgs e)
        {
            _chartFullScreenWindow = null;
            _chartFullScreenView = null;
            _isFullScreen = false;
        }

        private void ExitChartFullScreen()
        {
            Window? window = _chartFullScreenWindow;
            _chartFullScreenWindow = null;

            if (window != null)
                window.Close();

            _chartFullScreenView = null;
            _isFullScreen = false;
            FullScreenExitButton.Visibility = Visibility.Collapsed;
        }

        private void ExitFullScreen()
        {
            ExitChartFullScreen();
        }

        private void CloseChartFullScreenIfOpen()
        {
            ExitChartFullScreen();
        }
    }
}
