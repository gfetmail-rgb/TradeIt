using System;
using System.Windows;
using System.Windows.Controls;
using TradeIt.Charts;

namespace TradeIt
{
    public partial class MainWindow
    {
        private static readonly bool _fullScreenFixRegistered = RegisterFullScreenFix();
        private Window? _chartFullScreenWindow;
        private TabItem? _chartFullScreenTab;
        private object? _chartFullScreenOriginalContent;

        private static bool RegisterFullScreenFix()
        {
            EventManager.RegisterClassHandler(typeof(MainWindow), Button.ClickEvent, new RoutedEventHandler(FullScreenFix_ButtonClick), true);
            return true;
        }

        private static void FullScreenFix_ButtonClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not Button button) return;
            MainWindow? window = Window.GetWindow(button) as MainWindow;
            if (window == null) return;
            if (ReferenceEquals(button, window.FullScreenButton))
            {
                e.Handled = true;
                window.EnterChartFullScreen();
            }
            else if (ReferenceEquals(button, window.FullScreenExitButton))
            {
                e.Handled = true;
                window.ExitChartFullScreen();
            }
        }

        private void EnterChartFullScreen()
        {
            if (_chartFullScreenWindow != null) return;
            if (ChartTabs.SelectedItem is not TabItem tab || tab.Content is not ChartTabView chart) return;

            try
            {
                _chartFullScreenTab = tab;
                _chartFullScreenOriginalContent = tab.Content;
                tab.Content = null;

                var host = new Grid();
                host.Children.Add(chart);

                var exitButton = new Button
                {
                    Content = "↙ خروج از تمام صفحه",
                    Width = 175,
                    Height = 34,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 10, 10, 0),
                    Padding = new Thickness(10, 0)
                };
                exitButton.Click += (_, _) => ExitChartFullScreen();
                Panel.SetZIndex(exitButton, 10000);
                host.Children.Add(exitButton);

                var fullScreenWindow = new Window
                {
                    Title = $"TradeIt — {tab.Header}",
                    Owner = this,
                    WindowStyle = WindowStyle.None,
                    ResizeMode = ResizeMode.NoResize,
                    WindowState = WindowState.Maximized,
                    ShowInTaskbar = false,
                    Background = System.Windows.Media.Brushes.Black,
                    Content = host
                };

                fullScreenWindow.KeyDown += ChartFullScreenWindow_KeyDown;
                fullScreenWindow.Closed += ChartFullScreenWindow_Closed;
                _chartFullScreenWindow = fullScreenWindow;
                _isFullScreen = true;
                Visibility = Visibility.Hidden;
                fullScreenWindow.Show();
                fullScreenWindow.Activate();
                fullScreenWindow.Focus();
            }
            catch (Exception ex)
            {
                if (_chartFullScreenTab != null) _chartFullScreenTab.Content = _chartFullScreenOriginalContent;
                _chartFullScreenTab = null;
                _chartFullScreenOriginalContent = null;
                _chartFullScreenWindow = null;
                _isFullScreen = false;
                Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine($"Fullscreen chart failed: {ex}");
            }
        }

        private void ChartFullScreenWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                e.Handled = true;
                ExitChartFullScreen();
            }
        }

        private void ChartFullScreenWindow_Closed(object? sender, EventArgs e)
        {
            if (_isFullScreen) RestoreChartFromFullScreen();
        }

        private void ExitChartFullScreen()
        {
            if (_chartFullScreenWindow == null) return;
            try
            {
                _chartFullScreenWindow.Closed -= ChartFullScreenWindow_Closed;
                _chartFullScreenWindow.KeyDown -= ChartFullScreenWindow_KeyDown;
                _chartFullScreenWindow.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fullscreen window close failed: {ex}");
                RestoreChartFromFullScreen();
            }
        }

        private void ExitFullScreen()
        {
            ExitChartFullScreen();
        }

        private void RestoreChartFromFullScreen()
        {
            if (_chartFullScreenTab != null) _chartFullScreenTab.Content = _chartFullScreenOriginalContent;
            _chartFullScreenTab = null;
            _chartFullScreenOriginalContent = null;
            _chartFullScreenWindow = null;
            _isFullScreen = false;
            Visibility = Visibility.Visible;
            Activate();
        }
    }
}
