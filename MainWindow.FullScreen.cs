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
        // Fullscreen deliberately uses a NEW ChartTabView instance.
        // Moving the live ScottPlot WpfPlot between visual trees was the source
        // of the blank-window problem. The original chart remains untouched in
        // its tab, and the fullscreen chart is a fresh rendering of the same data.
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
                // IMPORTANT: do not remove the original ChartTabView from its tab.
                // Create a completely independent view so ScottPlot gets a normal
                // WPF lifecycle in the fullscreen window.
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
                    Background = Brushes.White
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
                    Background = Brushes.White,
                    Content = host
                };

                window.KeyDown += FullScreenWindow_KeyDown;
                window.Closed += FullScreenWindow_Closed;

                _chartFullScreenWindow = window;
                window.Show();
                window.Activate();

                // The clone has its own ScottPlot control and receives a fresh
                // Loaded/layout cycle. Refresh once after layout is established.
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

            // The MainWindow itself was never modified, so there is nothing to
            // restore. Keep the normal maximized application exactly as it was.
            FullScreenExitButton.Visibility = Visibility.Collapsed;
        }

        // MainWindow.PreviewKeyDown calls this compatibility name.
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
