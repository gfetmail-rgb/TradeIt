using System.Windows;
using System.Windows.Controls;

namespace TradeIt
{
    public partial class MainWindow
    {
        private bool _suppressInitialPortfolioSelection = true;

        private static readonly bool _startupSelectionHandlerRegistered = RegisterStartupSelectionHandler();

        private static bool RegisterStartupSelectionHandler()
        {
            EventManager.RegisterClassHandler(
                typeof(ComboBox),
                ComboBox.SelectionChangedEvent,
                new SelectionChangedEventHandler(SuppressInitialPortfolioSelection));
            return true;
        }

        private static void SuppressInitialPortfolioSelection(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox combo || combo.Name != "PortfolioComboBox")
                return;

            if (Window.GetWindow(combo) is not MainWindow window)
                return;

            if (!window._suppressInitialPortfolioSelection)
                return;

            if (combo.SelectedIndex >= 0)
            {
                window._suppressInitialPortfolioSelection = false;
                combo.SelectedIndex = -1;
            }
        }
    }
}
