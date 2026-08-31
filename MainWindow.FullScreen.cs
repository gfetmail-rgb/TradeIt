using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TradeIt.Charts;

namespace TradeIt
{
    public partial class MainWindow
    {
        // =========================================================
        // CHART FULL SCREEN — DISABLED TEMPORARILY
        // =========================================================
        // The previous implementation is intentionally disabled so that
        // neither chart fullscreen nor a second fullscreen window can run.
        // It is kept below under #if false as a rollback/reference point.
        // =========================================================

#if false
        private Window? _chartFullScreenWindow;
        private ChartTabView? _chartFullScreenView;

        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (_chartFullScreenWindow != null)
                return;

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

                var exitButton = new System.Windows.Controls.Button
                {
                    Content = "↙ خروج از تمام صفحه",
                    Width = 175,
                    Height = 36,
                    Padding = new Thickness(10, 0),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 10, 10, 0),
                    Focusable = true
                };
                exitButton.Click += FullScreenWindowExitButton_Click;

                var host = new Grid
                {
                    Background = System.Windows.Media.Brushes.White
                };
                host.Children.Add(fullScreenChart);
                host.Children.Add(exitButton);
                Panel.SetZIndex(exitButton, 10000);

                var window = new Window
                {
                    Title = "TradeIt - نمودار",
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    WindowState = WindowState.Maximized,
                    WindowStyle = WindowStyle.None,
                    ResizeMode = ResizeMode.NoResize,
                    ShowInTaskbar = true,
                    Background = System.Windows.Media.Brushes.White,
                    Content = host
                };

                window.KeyDown += FullScreenWindow_KeyDown;
                window.Closed += FullScreenWindow_Closed;

                _chartFullScreenWindow = window;
                window.Show();
                window.Activate();
                window.UpdateLayout();
                fullScreenChart.UpdateLayout();
                fullScreenChart.Chart.UpdateLayout();
                fullScreenChart.Chart.Refresh();
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

        private void FullScreenWindowExitButton_Click(object sender, RoutedEventArgs e)
        {
            ExitChartFullScreen();
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
#endif

        // No-op handlers remain so existing XAML/event hookups continue to
        // compile, but they cannot open or close a fullscreen chart.
        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            // Fullscreen intentionally disabled.
        }

        private void FullScreenExitButton_Click(object sender, RoutedEventArgs e)
        {
            // Fullscreen intentionally disabled.
        }

        private void ExitFullScreen()
        {
            // Fullscreen intentionally disabled.
        }

        private void CloseChartFullScreenIfOpen()
        {
            // Fullscreen intentionally disabled.
        }
    }
}
