using System.Windows;
using System.Windows.Controls;

namespace TradeIt
{
    public partial class MainWindow
    {
        private static readonly bool _dateFilterAvailabilityHandlerRegistered = RegisterDateFilterAvailabilityHandler();

        private static bool RegisterDateFilterAvailabilityHandler()
        {
            EventManager.RegisterClassHandler(typeof(MainWindow), Window.LoadedEvent, new RoutedEventHandler(UpdateDateFilterAvailabilityOnLoaded));
            EventManager.RegisterClassHandler(typeof(ComboBox), ComboBox.SelectionChangedEvent, new SelectionChangedEventHandler(UpdateDateFilterAvailabilityOnPortfolioChanged));
            return true;
        }

        private static void UpdateDateFilterAvailabilityOnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is MainWindow window)
                window.Dispatcher.BeginInvoke(new Action(window.UpdateDateFilterAvailability), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private static void UpdateDateFilterAvailabilityOnPortfolioChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox combo || combo.Name != "PortfolioComboBox")
                return;
            if (Window.GetWindow(combo) is MainWindow window)
                window.Dispatcher.BeginInvoke(new Action(window.UpdateDateFilterAvailability), System.Windows.Threading.DispatcherPriority.DataBind);
        }

        private void UpdateDateFilterAvailability()
        {
            bool hasDateData = _selectedPortfolio?.DataSource?.HasDateTime == true;

            if (_daysWithoutTradeCheckBox != null)
                _daysWithoutTradeCheckBox.IsEnabled = hasDateData;
            if (_daysWithoutTradeTextBox != null)
                _daysWithoutTradeTextBox.IsEnabled = hasDateData;
            if (_daysWithTradeCheckBox != null)
                _daysWithTradeCheckBox.IsEnabled = hasDateData;
            if (_daysWithTradeTextBox != null)
                _daysWithTradeTextBox.IsEnabled = hasDateData;
        }
    }
}
