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
        private object? _chartFullScreenOriginalContent;
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
            _chartFullScreenOriginalContent = tab.Content;
            _chartFullScreenOriginalHeader = tab.Header;

            tab.Content = null;

            var root = new WpfGrid();
            root.Children.Add(chartView);

            var exitButton = new WpfButton
            {
                Content = "↙ خروج از تمام صفحه",
                Width = 175,
                Height = 38,
                Padding = new Thickness(10, 0, 10, 0),
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
            if (_chartFullScreenWindow == null)
                return;

            _chartFullScreenWindow.Closed -= ChartFullScreenWindow_Closed;
            _chartFullScreenWindow.Close();
            RestoreChartFromFullScreen();
        }

        private void ChartFullScreenWindow_Closed(object? sender, EventArgs e)
        {
            RestoreChartFromFullScreen();
        }

        private void RestoreChartFromFullScreen()
        {
            if (_chartFullScreenTab != null && _chartFullScreenOriginalContent != null)
            {
                _chartFullScreenTab.Content = _chartFullScreenOriginalContent;
                _chartFullScreenTab.Header = _chartFullScreenOriginalHeader;
                ChartTabs.SelectedItem = _chartFullScreenTab;
            }

            _chartFullScreenWindow = null;
            _chartFullScreenTab = null;
            _chartFullScreenOriginalContent = null;
            _chartFullScreenOriginalHeader = null;
        }
    }
}
