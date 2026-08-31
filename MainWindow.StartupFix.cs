using System;
using System.Windows;
using System.Windows.Threading;

namespace TradeIt
{
    public partial class MainWindow
    {
        static MainWindow()
        {
            // Startup state forcing is intentionally disabled.
            // The application must not force Maximized mode here.
            /*
            EventManager.RegisterClassHandler(
                typeof(MainWindow),
                FrameworkElement.InitializedEvent,
                new RoutedEventHandler(MainWindow_StartupWindowInitialized));

            EventManager.RegisterClassHandler(
                typeof(MainWindow),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(MainWindow_StartupFixLoaded));
            */
        }

        // Startup Maximized code intentionally disabled.
        /*
        private static void MainWindow_StartupWindowInitialized(object sender, RoutedEventArgs e)
        {
            if (sender is not MainWindow window)
                return;

            window._isFullScreen = false;
            window.WindowStyle = WindowStyle.SingleBorderWindow;
            window.ResizeMode = ResizeMode.CanResize;
            window.WindowState = WindowState.Maximized;
        }

        private static void MainWindow_StartupFixLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not MainWindow window)
                return;

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
        */

        // Keep this method available for any existing references, but do not
        // force any window state from it.
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
