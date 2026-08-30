using System;
using System.Windows;
using System.Windows.Controls;

namespace TradeIt
{
    public partial class MainWindow
    {
        // Chart fullscreen is a layout mode of the existing MainWindow.
        // It must NEVER change WindowStyle or create/re-parent a chart Window.

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

            ChartArea.Visibility = Visibility.Visible;
            ChartTabs.Visibility = Visibility.Visible;

            FullScreenExitButton.Visibility = Visibility.Visible;
            Panel.SetZIndex(FullScreenExitButton, 10000);

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

        private void CloseChartFullScreenIfOpen()
        {
            if (_isFullScreen)
                ExitChartFullScreen();
        }
    }
}
