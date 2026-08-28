using System.Windows;

namespace TradeIt
{
    public partial class MainWindow
    {
        private static readonly bool _startupWindowStateHandlerRegistered = RegisterStartupWindowStateHandler();

        private static bool RegisterStartupWindowStateHandler()
        {
            EventManager.RegisterClassHandler(
                typeof(MainWindow),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(EnsureStartupWindowState),
                true);
            return true;
        }

        private static void EnsureStartupWindowState(object sender, RoutedEventArgs e)
        {
            if (sender is MainWindow window)
                window.WindowState = WindowState.Maximized;
        }
    }
}