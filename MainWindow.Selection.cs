using System.Windows;

namespace TradeIt
{
    public partial class MainWindow
    {
        // CheckBox selection is independent from opening a chart.
        // Keep the All/None indicators synchronized with the current
        // CheckBox state without changing the chart-click behavior.
        private void SymbolCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressSymbolSelection)
                return;

            UpdateSelectionRadioButtons();
        }

        private void SymbolCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressSymbolSelection)
                return;

            UpdateSelectionRadioButtons();
        }
    }
}
