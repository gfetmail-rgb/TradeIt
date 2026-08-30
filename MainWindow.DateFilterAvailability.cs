using System;
using System.Windows;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfSelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;
using WpfSelectionChangedEventHandler = System.Windows.Controls.SelectionChangedEventHandler;

namespace TradeIt
{
    public partial class MainWindow
    {
        private static readonly bool _dateFilterAvailabilityHandlerRegistered = RegisterDateFilterAvailabilityHandler();

        private static bool RegisterDateFilterAvailabilityHandler()
        {
            EventManager.RegisterClassHandler(typeof(MainWindow), Window.LoadedEvent, new RoutedEventHandler(UpdateDateFilterAvailabilityOnLoaded));
            EventManager.RegisterClassHandler(typeof(WpfComboBox), WpfComboBox.SelectionChangedEvent, new WpfSelectionChangedEventHandler(UpdateDateFilterAvailabilityOnPortfolioChanged));
            return true;
        }

        private static void UpdateDateFilterAvailabilityOnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is MainWindow window)
                window.Dispatcher.BeginInvoke(new Action(window.UpdateDateFilterAvailability), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private static void UpdateDateFilterAvailabilityOnPortfolioChanged(object sender, WpfSelectionChangedEventArgs e)
        {
            if (sender is not WpfComboBox combo || combo.Name != "PortfolioComboBox")
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
