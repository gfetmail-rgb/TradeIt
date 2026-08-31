using System;
using System.Windows;
using WpfButton = System.Windows.Controls.Button;
using WpfGrid = System.Windows.Controls.Grid;
using WpfPanel = System.Windows.Controls.Panel;
using WpfTabItem = System.Windows.Controls.TabItem;
using WpfKey = System.Windows.Input.Key;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using TradeIt.Charts;

namespace TradeIt
{
    public partial class MainWindow
    {
        private Window? _chartFullScreenWindow;
        private WpfTabItem? _chartFullScreenTab;
        private ChartTabView? _chartFullScreenView;
        private object? _chartFullScreenOriginalHeader;

        private void ChartFullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (_chartFullScreenWindow != null)
                return;

            if (ChartTabs.SelectedItem is not WpfTabItem tab)
                return;

            if (tab.Content is not ChartTabView chartView)
                return;

            _chartFullScreenTab = tab;
            _chartFullScreenView = chartView;
            _chartFullScreenOriginalHeader = tab.Header;

            // Detach the ChartTabView from the TabItem BEFORE putting it in the
            // fullscreen window. This prevents WPF visual-tree parent conflicts
            // when the fullscreen window is closed.
            tab.Content = null;

            var root = new WpfGrid();
            root.Children.Add(chartView);

            var exitButton = new WpfButton
            {
                Content = "خروج",
                Width = 80,
                Height = 36,
                Padding = new Thickness(8, 0, 8, 0),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Margin = new Thickness(0, 12, 12, 0)
            };

            WpfPanel.SetZIndex(exitButton, 10000);
            exitButton.Click += (_, _) => ExitChartFullScreen();
            root.Children.Add(exitButton);

            var window = new Window
            {
                Title = $"TradeIt - {tab.Header}",
                Content = root,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                WindowState = WindowState.Maximized,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ShowInTaskbar = true,
                Owner = this
            };

            window.KeyDown += ChartFullScreenWindow_KeyDown;
            window.Closed += ChartFullScreenWindow_Closed;

            _chartFullScreenWindow = window;
            window.Show();
            window.Activate();
            window.Focus();
        }

        private void ChartFullScreenWindow_KeyDown(object sender, WpfKeyEventArgs e)
        {
            if (e.Key == WpfKey.Escape)
            {
                ExitChartFullScreen();
                e.Handled = true;
            }
        }

        private void ExitChartFullScreen()
        {
            var window = _chartFullScreenWindow;
            if (window == null)
                return;

            // Detach the ChartTabView from the fullscreen Window's visual tree
            // BEFORE closing the Window. This is the critical part that avoids
            // ArgumentException: "Must disconnect specified child from current parent Visual..."
            if (_chartFullScreenView != null && ReferenceEquals(window.Content, _chartFullScreenView.Parent))
            {
                // Normally the direct parent is the Grid, so handle it below.
            }

            if (_chartFullScreenView != null && window.Content is WpfGrid root)
            {
                root.Children.Remove(_chartFullScreenView);
            }

            window.Closed -= ChartFullScreenWindow_Closed;
            window.Close();
            RestoreChartFromFullScreen();
        }

        private void ChartFullScreenWindow_Closed(object? sender, EventArgs e)
        {
            if (_chartFullScreenView != null && sender is Window window && window.Content is WpfGrid root)
            {
                root.Children.Remove(_chartFullScreenView);
            }

            RestoreChartFromFullScreen();
        }

        private void RestoreChartFromFullScreen()
        {
            if (_chartFullScreenTab != null && _chartFullScreenView != null)
            {
                _chartFullScreenTab.Content = _chartFullScreenView;
                _chartFullScreenTab.Header = _chartFullScreenOriginalHeader;
                ChartTabs.SelectedItem = _chartFullScreenTab;
            }

            _chartFullScreenWindow = null;
            _chartFullScreenTab = null;
            _chartFullScreenView = null;
            _chartFullScreenOriginalHeader = null;
        }
    }
}
