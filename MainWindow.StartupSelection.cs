using System.Windows;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfSelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;

namespace TradeIt
{
    public partial class MainWindow
    {
        private bool _suppressInitialPortfolioSelection = true;

        private static readonly bool _startupSelectionHandlerRegistered = RegisterStartupSelectionHandler();

        private static bool RegisterStartupSelectionHandler()
        {
            EventManager.RegisterClassHandler(
                typeof(WpfComboBox),
                WpfComboBox.SelectionChangedEvent,
                new WpfSelectionChangedEventHandler(SuppressInitialPortfolioSelection));
            return true;
        }

        private static void SuppressInitialPortfolioSelection(object sender, WpfSelectionChangedEventArgs e)
        {
            if (sender is not WpfComboBox combo || combo.Name != "PortfolioComboBox")
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
