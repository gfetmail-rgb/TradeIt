using System;
using System.Windows;
using System.Windows.Threading;

namespace TradeIt
{
    public partial class MainWindow
    {
        static MainWindow()
        {
            // Initialized runs early enough to establish the application
            // window state before normal Loaded processing begins.
            EventManager.RegisterClassHandler(
                typeof(MainWindow),
                FrameworkElement.InitializedEvent,
                new RoutedEventHandler(MainWindow_StartupWindowInitialized));

            EventManager.RegisterClassHandler(
                typeof(MainWindow),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(MainWindow_StartupFixLoaded));
        }

        private static void MainWindow_StartupWindowInitialized(object sender, RoutedEventArgs e)
        {
            if (sender is not MainWindow window)
                return;

            // Startup is NORMAL MAXIMIZED application mode.
            // Chart fullscreen is an explicit user action only.
            window._isFullScreen = false;
            window.WindowStyle = WindowStyle.SingleBorderWindow;
            window.ResizeMode = ResizeMode.CanResize;
            window.WindowState = WindowState.Maximized;
        }

        private static void MainWindow_StartupFixLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not MainWindow window)
                return;

            // Re-assert the normal state after all Loaded handlers have run.
            window._isFullScreen = false;
            window.WindowStyle = WindowStyle.SingleBorderWindow;
            window.ResizeMode = ResizeMode.CanResize;
            window.WindowState = WindowState.Maximized;

            if (window.FullScreenExitButton != null)
                window.FullScreenExitButton.Visibility = Visibility.Collapsed;

            window.TopToolbar.Visibility = Visibility.Visible;
            window.StatusBar.Visibility = Visibility.Visible;
            window.SymbolsPanel.Visibility = Visibility.Visible;
            window.MainContent.Visibility = Visibility.Visible;
            window.ChartArea.Visibility = Visibility.Visible;

            window.Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(window.ClearStartupSelection));
        }

        private void ClearStartupSelection()
        {
            if (PortfolioComboBox == null || SymbolsDataGrid == null)
                return;

            PortfolioComboBox.SelectedItem = null;
            PortfolioComboBox.SelectedIndex = -1;

            _selectedPortfolio = null;
            _allSymbols.Clear();
            SymbolsDataGrid.ItemsSource = null;
            SymbolsDataGrid.SelectedItem = null;

            StatusTextBlock.Text = _portfolios.Count > 0
                ? "یک سبد را انتخاب کنید."
                : "هنوز سبدی تعریف نشده است.";
        }
    }
}
