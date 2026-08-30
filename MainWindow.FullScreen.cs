using System.Windows;
using System.Windows.Controls;

namespace TradeIt
{
    public partial class MainWindow
    {
        // Chart fullscreen is implemented inside the existing MainWindow.
        // MainContent is NOT moved between Grid rows. This is important for
        // ScottPlot/WPF rendering and prevents the blank-chart problem.

        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isFullScreen)
                return;

            if (ChartTabs.SelectedItem is not TabItem)
            {
                WpfMessageBox.Show(
                    "ابتدا یک چارت را باز و انتخاب کنید.",
                    "تمام صفحه",
                    WpfMessageBoxButton.OK,
                    WpfMessageBoxImage.Information);
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

            // Save the current root layout. MainContent itself stays in row 1.
            _previousRootRow0Height = TopToolbarRow.Height;
            _previousRootRow1Height = MainContentRow.Height;
            _previousRootRow2Height = StatusBarRow.Height;

            _previousSymbolsColumnWidth = SymbolsPanelColumn.Width;
            _previousChartColumnWidth = ChartPanelColumn.Width;

            _previousWindowState = WindowState;
            _previousWindowStyle = WindowStyle;
            _previousResizeMode = ResizeMode;

            _isFullScreen = true;

            // Hide the normal application chrome.
            TopToolbar.Visibility = Visibility.Collapsed;
            StatusBar.Visibility = Visibility.Collapsed;
            SymbolsPanel.Visibility = Visibility.Collapsed;

            // Keep MainContent in its original Grid row, but make that row
            // occupy the entire client area.
            TopToolbarRow.Height = new GridLength(0);
            MainContentRow.Height = new GridLength(1, GridUnitType.Star);
            StatusBarRow.Height = new GridLength(0);

            SymbolsPanelColumn.Width = new GridLength(0);
            ChartPanelColumn.Width = new GridLength(1, GridUnitType.Star);

            // Real window fullscreen: no title bar/borders, maximized.
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;

            ChartArea.Visibility = Visibility.Visible;
            ChartTabs.Visibility = Visibility.Visible;
            FullScreenExitButton.Visibility = Visibility.Visible;
            Panel.SetZIndex(FullScreenExitButton, 10000);

            UpdateLayout();
            MainContent.UpdateLayout();
            ChartArea.UpdateLayout();
            ChartTabs.UpdateLayout();

            // ScottPlot keeps its existing visual tree. Only force a normal
            // WPF layout/visual refresh after the available size changes.
            if (ChartTabs.SelectedItem is TabItem tab &&
                tab.Content is FrameworkElement content)
            {
                content.Visibility = Visibility.Visible;
                content.UpdateLayout();
                content.InvalidateMeasure();
                content.InvalidateArrange();
                content.InvalidateVisual();
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

            // Restore root layout exactly as it was.
            TopToolbarRow.Height = _previousRootRow0Height;
            MainContentRow.Height = _previousRootRow1Height;
            StatusBarRow.Height = _previousRootRow2Height;

            SymbolsPanelColumn.Width = _previousSymbolsColumnWidth;
            ChartPanelColumn.Width = _previousChartColumnWidth;

            TopToolbar.Visibility = Visibility.Visible;
            StatusBar.Visibility = Visibility.Visible;
            SymbolsPanel.Visibility = Visibility.Visible;

            ChartArea.Visibility = Visibility.Visible;
            ChartTabs.Visibility = Visibility.Visible;

            WindowStyle = _previousWindowStyle;
            ResizeMode = _previousResizeMode;
            WindowState = _previousWindowState;

            UpdateLayout();
            MainContent.UpdateLayout();
            ChartArea.UpdateLayout();
            ChartTabs.UpdateLayout();

            if (ChartTabs.SelectedItem is TabItem tab &&
                tab.Content is FrameworkElement content)
            {
                content.UpdateLayout();
                content.InvalidateMeasure();
                content.InvalidateArrange();
                content.InvalidateVisual();
            }
        }

        private void CloseChartFullScreenIfOpen()
        {
            if (_isFullScreen)
                ExitChartFullScreen();
        }
    }
}
