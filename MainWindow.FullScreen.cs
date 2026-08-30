using System.Windows;
using System.Windows.Controls;

namespace TradeIt
{
    public partial class MainWindow
    {
        // Chart fullscreen is implemented by changing the layout of the
        // existing MainWindow. No second Window is created and no chart
        // control is re-parented. This prevents the blank-window problem.

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

            // Save the actual position of MainContent in RootLayout.
            _previousMainContentRow = Grid.GetRow(MainContent);
            _previousMainContentColumn = Grid.GetColumn(MainContent);
            _previousMainContentRowSpan = Grid.GetRowSpan(MainContent);
            _previousMainContentColumnSpan = Grid.GetColumnSpan(MainContent);

            _previousSymbolsColumnWidth = SymbolsPanelColumn.Width;
            _previousChartColumnWidth = ChartPanelColumn.Width;

            _isFullScreen = true;

            // MainContent now occupies the complete client area of the
            // existing maximized MainWindow. The other root-level controls
            // are hidden, while ChartArea remains the same visual tree.
            TopToolbar.Visibility = Visibility.Collapsed;
            StatusBar.Visibility = Visibility.Collapsed;
            SymbolsPanel.Visibility = Visibility.Collapsed;

            Grid.SetRow(MainContent, 0);
            Grid.SetRowSpan(MainContent, 3);
            Grid.SetColumn(MainContent, 0);
            Grid.SetColumnSpan(MainContent, 2);

            SymbolsPanelColumn.Width = new GridLength(0);
            ChartPanelColumn.Width = new GridLength(1, GridUnitType.Star);

            ChartArea.Visibility = Visibility.Visible;
            ChartTabs.Visibility = Visibility.Visible;

            FullScreenExitButton.Visibility = Visibility.Visible;
            Panel.SetZIndex(FullScreenExitButton, 10000);

            MainContent.UpdateLayout();
            ChartArea.UpdateLayout();
            ChartTabs.UpdateLayout();

            // Force the selected chart control to recalculate its WPF size.
            if (ChartTabs.SelectedItem is TabItem tab && tab.Content is FrameworkElement content)
            {
                content.Visibility = Visibility.Visible;
                content.UpdateLayout();
                content.InvalidateVisual();
                content.Focus();
            }
        }

        // Kept because MainWindow_PreviewKeyDown already calls this name.
        private void ExitFullScreen()
        {
            ExitChartFullScreen();
        }

        private void ExitChartFullScreen()
        {
            if (!_isFullScreen)
                return;

            _isFullScreen = false;
            FullScreenExitButton.Visibility = Visibility.Collapsed;

            // Restore MainContent exactly where it was before fullscreen.
            Grid.SetRow(MainContent, _previousMainContentRow);
            Grid.SetColumn(MainContent, _previousMainContentColumn);
            Grid.SetRowSpan(MainContent, _previousMainContentRowSpan);
            Grid.SetColumnSpan(MainContent, _previousMainContentColumnSpan);

            SymbolsPanelColumn.Width = _previousSymbolsColumnWidth;
            ChartPanelColumn.Width = _previousChartColumnWidth;

            TopToolbar.Visibility = Visibility.Visible;
            StatusBar.Visibility = Visibility.Visible;
            SymbolsPanel.Visibility = Visibility.Visible;

            ChartArea.Visibility = Visibility.Visible;
            ChartTabs.Visibility = Visibility.Visible;

            MainContent.UpdateLayout();
            ChartArea.UpdateLayout();
            ChartTabs.UpdateLayout();

            if (ChartTabs.SelectedItem is TabItem tab && tab.Content is FrameworkElement content)
            {
                content.UpdateLayout();
                content.InvalidateVisual();
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
