using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TradeIt
{
    public partial class MainWindow
    {
        private Window? _chartFullScreenWindow;
        private TabItem? _chartFullScreenTab;
        private object? _chartFullScreenContent;

        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            EnterFullScreen();
        }

        private void FullScreenExitButton_Click(object sender, RoutedEventArgs e)
        {
            ExitFullScreen();
        }

        private void EnterFullScreen()
        {
            if (_chartFullScreenWindow != null)
                return;

            if (ChartTabs.SelectedItem is not TabItem tab || tab.Content is not UIElement chart)
            {
                MessageBox.Show(
                    "ابتدا یک چارت را باز و انتخاب کنید.",
                    "تمام صفحه",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            _chartFullScreenTab = tab;
            _chartFullScreenContent = chart;
            tab.Content = null;

            var exitButton = new Button
            {
                Content = "↙ خروج از تمام صفحه",
                Width = 175,
                Height = 34,
                Padding = new Thickness(10, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 10, 10, 0)
            };

            exitButton.Click += FullScreenExitButton_Click;

            var root = new Grid
            {
                Background = Brushes.White
            };

            root.Children.Add(chart);
            root.Children.Add(exitButton);

            var window = new Window
            {
                Title = "TradeIt - Chart",
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                WindowState = WindowState.Maximized,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = Brushes.White,
                Content = root,
                Owner = this,
                ShowInTaskbar = false
            };

            _chartFullScreenWindow = window;
            _isFullScreen = true;

            if (FullScreenButton != null)
                FullScreenButton.Visibility = Visibility.Collapsed;

            window.KeyDown += ChartFullScreenWindow_KeyDown;
            window.Closed += ChartFullScreenWindow_Closed;

            window.Show();
            window.Activate();
            chart.Focus();
        }

        private void ChartFullScreenWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ExitFullScreen();
                e.Handled = true;
            }
        }

        private void ChartFullScreenWindow_Closed(object? sender, EventArgs e)
        {
            RestoreChartFromFullScreen();
        }

        private void ExitFullScreen()
        {
            if (_chartFullScreenWindow == null)
                return;

            _chartFullScreenWindow.Close();
        }

        private void RestoreChartFromFullScreen()
        {
            if (_chartFullScreenTab != null && _chartFullScreenContent is UIElement chart)
                _chartFullScreenTab.Content = chart;

            _chartFullScreenTab = null;
            _chartFullScreenContent = null;
            _chartFullScreenWindow = null;
            _isFullScreen = false;

            if (FullScreenButton != null)
                FullScreenButton.Visibility = Visibility.Visible;
        }

        private void CloseChartFullScreenIfOpen()
        {
            if (_chartFullScreenWindow != null)
                ExitFullScreen();
        }
    }
}
