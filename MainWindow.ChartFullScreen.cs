using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TradeIt.Charts;

namespace TradeIt
{
    public partial class MainWindow
    {
        private Window? _chartFullScreenWindow;
        private TabItem? _chartFullScreenTab;
        private object? _chartFullScreenOriginalContent;
        private object? _chartFullScreenOriginalHeader;

        private void ChartFullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (_chartFullScreenWindow != null)
                return;

            if (ChartTabs.SelectedItem is not TabItem tab)
                return;

            if (tab.Content is not ChartTabView chartView)
                return;

            _chartFullScreenTab = tab;
            _chartFullScreenOriginalContent = tab.Content;
            _chartFullScreenOriginalHeader = tab.Header;

            tab.Content = null;

            var root = new Grid();
            root.Children.Add(chartView);

            var exitButton = new Button
            {
                Content = "↙ خروج از تمام صفحه",
                Width = 175,
                Height = 38,
                Padding = new Thickness(10, 0, 10, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 12, 12, 0)
            };

            Panel.SetZIndex(exitButton, 10000);
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

        private void ChartFullScreenWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
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

        private void ChartFullScreenWindow_Closed(object? sender, System.EventArgs e)
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
