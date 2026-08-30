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
        // The selected ChartTabView is temporarily hosted in a dedicated
        // borderless maximized WPF window. The original chart instance is
        // preserved, so ScottPlot does not have to survive a MainWindow
        // Grid reconfiguration.
        private Window? _chartFullScreenWindow;
        private ChartTabView? _chartFullScreenView;
        private TabItem? _chartFullScreenSourceTab;

        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (_chartFullScreenWindow != null)
                return;

            if (ChartTabs.SelectedItem is not TabItem tab ||
                tab.Content is not ChartTabView chartView)
            {
                WpfMessageBox.Show(
                    "ابتدا یک چارت را باز و انتخاب کنید.",
                    "تمام صفحه",
                    WpfMessageBoxButton.OK,
                    WpfMessageBoxImage.Information);
                return;
            }

            EnterChartFullScreen(tab, chartView);
        }

        private void FullScreenExitButton_Click(object sender, RoutedEventArgs e)
        {
            ExitChartFullScreen();
        }

        private void EnterChartFullScreen(TabItem sourceTab, ChartTabView chartView)
        {
            if (_chartFullScreenWindow != null)
                return;

            _chartFullScreenSourceTab = sourceTab;
            _chartFullScreenView = chartView;
            _isFullScreen = true;

            sourceTab.Content = null;

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
            host.Children.Add(chartView);
            host.Children.Add(exitButton);
            Panel.SetZIndex(exitButton, 10000);

            var window = new Window
            {
                Title = $"TradeIt - {sourceTab.Header}",
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                WindowState = WindowState.Maximized,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Background = Brushes.White,
                Content = host
            };

            window.KeyDown += FullScreenWindow_KeyDown;
            window.Closed += FullScreenWindow_Closed;

            _chartFullScreenWindow = window;
            window.Show();

            // Do layout work after Show(), when the window has real dimensions.
            window.UpdateLayout();
            chartView.UpdateLayout();
            chartView.InvalidateMeasure();
            chartView.InvalidateArrange();
            chartView.InvalidateVisual();
            window.Activate();
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
            if (_isFullScreen)
            {
                _chartFullScreenWindow = null;
                RestoreChartFromFullScreen();
            }
        }

        private void ExitChartFullScreen()
        {
            var window = _chartFullScreenWindow;
            if (window == null)
                return;

            _chartFullScreenWindow = null;
            window.Close();
            RestoreChartFromFullScreen();
        }

        private void RestoreChartFromFullScreen()
        {
            if (_chartFullScreenView != null &&
                _chartFullScreenSourceTab != null)
            {
                _chartFullScreenSourceTab.Content = _chartFullScreenView;
                ChartTabs.SelectedItem = _chartFullScreenSourceTab;
            }

            _chartFullScreenView = null;
            _chartFullScreenSourceTab = null;
            _isFullScreen = false;

            // Explicitly restore the normal application state. This also
            // protects startup from any stale fullscreen state.
            FullScreenExitButton.Visibility = Visibility.Collapsed;
            TopToolbar.Visibility = Visibility.Visible;
            StatusBar.Visibility = Visibility.Visible;
            SymbolsPanel.Visibility = Visibility.Visible;
            MainContent.Visibility = Visibility.Visible;
            ChartArea.Visibility = Visibility.Visible;
            ChartTabs.Visibility = Visibility.Visible;

            TopToolbarRow.Height = new GridLength(55);
            MainContentRow.Height = new GridLength(1, GridUnitType.Star);
            StatusBarRow.Height = new GridLength(30);
            SymbolsPanelColumn.Width = new GridLength(300);
            ChartPanelColumn.Width = new GridLength(1, GridUnitType.Star);

            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            WindowState = WindowState.Maximized;

            UpdateLayout();
        }

        private void CloseChartFullScreenIfOpen()
        {
            ExitChartFullScreen();
        }
    }
}
