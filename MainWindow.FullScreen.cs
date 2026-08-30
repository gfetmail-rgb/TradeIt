using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TradeIt
{
    public partial class MainWindow
    {
        // Chart fullscreen is a layout mode of the existing MainWindow.
        // It must NEVER change WindowStyle or create/re-parent a chart Window.
        private bool _isFullScreen;

        private GridLength _savedTopToolbarHeight;
        private GridLength _savedMainContentHeight;
        private GridLength _savedStatusBarHeight;
        private GridLength _savedSymbolsPanelWidth;
        private GridLength _savedChartPanelWidth;

        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isFullScreen)
                return;

            if (ChartTabs.SelectedItem is not TabItem)
            {
                System.Windows.MessageBox.Show(
                    "ابتدا یک چارت را باز و انتخاب کنید.",
                    "تمام صفحه",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            EnterChartFullScreen();
        }

        private void FullScreenExitButton_Click(object sender, RoutedEventArgs e)
        {
            ExitChartFullScreen();
        }

        private void EnterChartFullScreen()
        {
            if (_isFullScreen)
                return;

            // Save only the layout dimensions. The native MainWindow state is
            // intentionally left untouched (normally it is already Maximized).
            _savedTopToolbarHeight = RootLayout.RowDefinitions[0].Height;
            _savedMainContentHeight = RootLayout.RowDefinitions[1].Height;
            _savedStatusBarHeight = RootLayout.RowDefinitions[2].Height;
            _savedSymbolsPanelWidth = SymbolsPanelColumn.Width;
            _savedChartPanelWidth = ChartPanelColumn.Width;

            _isFullScreen = true;

            TopToolbar.Visibility = Visibility.Collapsed;
            StatusBar.Visibility = Visibility.Collapsed;
            SymbolsPanel.Visibility = Visibility.Collapsed;

            RootLayout.RowDefinitions[0].Height = new GridLength(0);
            RootLayout.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
            RootLayout.RowDefinitions[2].Height = new GridLength(0);

            SymbolsPanelColumn.Width = new GridLength(0);
            ChartPanelColumn.Width = new GridLength(1, GridUnitType.Star);

            // ChartArea remains exactly where it was. Only its left panel is
            // collapsed, so ScottPlot is never removed from its visual tree.
            ChartArea.Visibility = Visibility.Visible;
            ChartTabs.Visibility = Visibility.Visible;

            FullScreenExitButton.Visibility = Visibility.Visible;
            Panel.SetZIndex(FullScreenExitButton, 10000);

            // Ensure the selected chart gets the newly available space.
            ChartArea.UpdateLayout();
            ChartTabs.UpdateLayout();

            if (ChartTabs.SelectedItem is TabItem tab && tab.Content is FrameworkElement content)
            {
                content.UpdateLayout();
                content.Focus();
            }
        }

        private void ExitChartFullScreen()
        {
            if (!_isFullScreen)
                return;

            _isFullScreen = false;

            FullScreenExitButton.Visibility = Visibility.Collapsed;

            SymbolsPanelColumn.Width = _savedSymbolsPanelWidth;
            ChartPanelColumn.Width = _savedChartPanelWidth;

            RootLayout.RowDefinitions[0].Height = _savedTopToolbarHeight;
            RootLayout.RowDefinitions[1].Height = _savedMainContentHeight;
            RootLayout.RowDefinitions[2].Height = _savedStatusBarHeight;

            SymbolsPanel.Visibility = Visibility.Visible;
            TopToolbar.Visibility = Visibility.Visible;
            StatusBar.Visibility = Visibility.Visible;

            ChartArea.Visibility = Visibility.Visible;
            ChartTabs.Visibility = Visibility.Visible;

            ChartArea.UpdateLayout();
            ChartTabs.UpdateLayout();

            if (ChartTabs.SelectedItem is TabItem tab && tab.Content is FrameworkElement content)
            {
                content.UpdateLayout();
                content.Focus();
            }
        }

        private void MainWindow_PreviewKeyDown_ChartFullScreen(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _isFullScreen)
            {
                ExitChartFullScreen();
                e.Handled = true;
            }
        }

        private void CloseChartFullScreenIfOpen()
        {
            if (_isFullScreen)
                ExitChartFullScreen();
        }
    }
}
