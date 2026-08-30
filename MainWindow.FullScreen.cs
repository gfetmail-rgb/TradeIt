using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace TradeIt
{
    public partial class MainWindow
    {
        // This implementation intentionally uses the existing MainWindow.
        // The chart control is NOT moved into another Window. Moving a ScottPlot
        // WpfPlot between visual trees can leave it blank after re-parenting.
        // Instead, fullscreen temporarily hides the surrounding MainWindow UI
        // and lets ChartArea occupy the whole window.

        private static readonly bool _fullscreenStartupHandlerRegistered = RegisterFullscreenStartupHandler();

        private static bool RegisterFullscreenStartupHandler()
        {
            EventManager.RegisterClassHandler(
                typeof(MainWindow),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(EnsureNormalStartupLayout),
                true);

            return true;
        }

        private static void EnsureNormalStartupLayout(object sender, RoutedEventArgs e)
        {
            if (sender is not MainWindow window)
                return;

            // Run after all other Loaded handlers so no fullscreen layout
            // accidentally survives application startup.
            window.Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(window.RestoreNormalMainLayout));
        }

        private void RestoreNormalMainLayout()
        {
            _isFullScreen = false;

            TopToolbar.Visibility = Visibility.Visible;
            StatusBar.Visibility = Visibility.Visible;
            SymbolsPanel.Visibility = Visibility.Visible;

            RootLayout.RowDefinitions[0].Height = new GridLength(55);
            RootLayout.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
            RootLayout.RowDefinitions[2].Height = new GridLength(30);

            Grid.SetRow(MainContent, 1);
            Grid.SetColumn(MainContent, 0);
            Grid.SetColumnSpan(MainContent, 2);

            Grid.SetColumn(SymbolsPanel, 0);
            Grid.SetColumn(ChartArea, 1);
            Grid.SetColumnSpan(ChartArea, 1);

            SymbolsPanelColumn.Width = new GridLength(300);
            ChartPanelColumn.Width = new GridLength(1, GridUnitType.Star);

            FullScreenExitButton.Visibility = Visibility.Collapsed;

            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            WindowState = WindowState.Maximized;
        }

        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            EnterFullScreen();
        }

        private void FullScreenExitButton_Click(object sender, RoutedEventArgs e)
        {
            ExitFullScreen();
        }

        private void EnterFullScreen()
        {
            if (_isFullScreen)
                return;

            if (ChartTabs.SelectedItem is not TabItem tab || tab.Content is not UIElement)
            {
                System.Windows.MessageBox.Show(
                    "ابتدا یک چارت را باز و انتخاب کنید.",
                    "تمام صفحه",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Save the exact MainWindow layout before changing it.
            _previousWindowState = WindowState;
            _previousWindowStyle = WindowStyle;
            _previousResizeMode = ResizeMode;

            _previousRootRow0Height = RootLayout.RowDefinitions[0].Height;
            _previousRootRow1Height = RootLayout.RowDefinitions[1].Height;
            _previousRootRow2Height = RootLayout.RowDefinitions[2].Height;

            _previousMainContentRow = Grid.GetRow(MainContent);
            _previousMainContentColumn = Grid.GetColumn(MainContent);
            _previousMainContentRowSpan = Grid.GetRowSpan(MainContent);
            _previousMainContentColumnSpan = Grid.GetColumnSpan(MainContent);

            _previousSymbolsColumnWidth = SymbolsPanelColumn.Width;
            _previousChartColumnWidth = ChartPanelColumn.Width;

            _isFullScreen = true;

            // Hide everything except the chart area.
            TopToolbar.Visibility = Visibility.Collapsed;
            StatusBar.Visibility = Visibility.Collapsed;
            SymbolsPanel.Visibility = Visibility.Collapsed;

            RootLayout.RowDefinitions[0].Height = new GridLength(0);
            RootLayout.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
            RootLayout.RowDefinitions[2].Height = new GridLength(0);

            // MainContent becomes the only visible content of RootLayout.
            Grid.SetRow(MainContent, 1);
            Grid.SetColumn(MainContent, 0);
            Grid.SetColumnSpan(MainContent, 2);
            Grid.SetRowSpan(MainContent, 1);

            // ChartArea fills the entire MainContent.
            Grid.SetColumn(ChartArea, 0);
            Grid.SetColumnSpan(ChartArea, 2);

            SymbolsPanelColumn.Width = new GridLength(0);
            ChartPanelColumn.Width = new GridLength(1, GridUnitType.Star);

            FullScreenExitButton.Visibility = Visibility.Visible;
            Panel.SetZIndex(FullScreenExitButton, 10000);

            // Keep the application maximized. This is chart fullscreen, not a
            // borderless second window and not a separate native fullscreen mode.
            WindowState = WindowState.Maximized;

            ChartTabs.SelectedItem = tab;
            tab.Content?.Focus();

            Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() =>
                {
                    if (_isFullScreen)
                    {
                        ChartTabs.UpdateLayout();
                        FullScreenExitButton.UpdateLayout();
                    }
                }));
        }

        private void ExitFullScreen()
        {
            if (!_isFullScreen)
                return;

            _isFullScreen = false;

            TopToolbar.Visibility = Visibility.Visible;
            StatusBar.Visibility = Visibility.Visible;
            SymbolsPanel.Visibility = Visibility.Visible;

            RootLayout.RowDefinitions[0].Height = _previousRootRow0Height;
            RootLayout.RowDefinitions[1].Height = _previousRootRow1Height;
            RootLayout.RowDefinitions[2].Height = _previousRootRow2Height;

            Grid.SetRow(MainContent, _previousMainContentRow);
            Grid.SetColumn(MainContent, _previousMainContentColumn);
            Grid.SetRowSpan(MainContent, _previousMainContentRowSpan);
            Grid.SetColumnSpan(MainContent, _previousMainContentColumnSpan);

            Grid.SetColumn(SymbolsPanel, 0);
            Grid.SetColumn(ChartArea, 1);
            Grid.SetColumnSpan(ChartArea, 1);

            SymbolsPanelColumn.Width = _previousSymbolsColumnWidth;
            ChartPanelColumn.Width = _previousChartColumnWidth;

            FullScreenExitButton.Visibility = Visibility.Collapsed;

            WindowStyle = _previousWindowStyle;
            ResizeMode = _previousResizeMode;
            WindowState = _previousWindowState == WindowState.Minimized
                ? WindowState.Maximized
                : _previousWindowState;

            Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() =>
                {
                    ChartTabs.UpdateLayout();
                    if (ChartTabs.SelectedItem is TabItem selected && selected.Content is FrameworkElement content)
                    {
                        content.UpdateLayout();
                        content.Focus();
                    }
                }));
        }

        private void CloseChartFullScreenIfOpen()
        {
            if (_isFullScreen)
                ExitFullScreen();
        }
    }
}
