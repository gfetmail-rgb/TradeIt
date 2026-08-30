using System.Windows;

namespace TradeIt
{
    public partial class MainWindow
    {
        private bool _symbolsPanelCollapsed;
        private bool _symbolTableGroupCollapsed;
        private bool _symbolFilterGroupCollapsed = true;
        private bool _symbolOperationsGroupCollapsed = true;

        private void SymbolTableCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            _symbolTableGroupCollapsed = !_symbolTableGroupCollapsed;
            SymbolsDataGrid.Visibility = _symbolTableGroupCollapsed ? Visibility.Collapsed : Visibility.Visible;
            SymbolTableCollapseButton.Content = _symbolTableGroupCollapsed ? "▼" : "▲";
        }

        private void SymbolFilterCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            _symbolFilterGroupCollapsed = !_symbolFilterGroupCollapsed;
            SymbolFilterHost.Visibility = _symbolFilterGroupCollapsed ? Visibility.Collapsed : Visibility.Visible;
            SymbolFilterCollapseButton.Content = _symbolFilterGroupCollapsed ? "▲" : "▼";
        }

        private void SymbolOperationsCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            _symbolOperationsGroupCollapsed = !_symbolOperationsGroupCollapsed;
            SymbolOperationsContent.Visibility = _symbolOperationsGroupCollapsed ? Visibility.Collapsed : Visibility.Visible;
            SymbolOperationsCollapseButton.Content = _symbolOperationsGroupCollapsed ? "▲" : "▼";
        }

        private void SymbolsPanelCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            _symbolsPanelCollapsed = !_symbolsPanelCollapsed;

            if (_symbolsPanelCollapsed)
            {
                SymbolsGroupsContainer.Visibility = Visibility.Collapsed;
                SymbolsPanelColumn.Width = new GridLength(32);
                SymbolsPanelCollapseButton.Content = "›";
                SymbolsPanelCollapseButton.ToolTip = "نمایش پنل نمادها";
            }
            else
            {
                SymbolsGroupsContainer.Visibility = Visibility.Visible;
                SymbolsPanelColumn.Width = new GridLength(300);
                SymbolsPanelCollapseButton.Content = "‹";
                SymbolsPanelCollapseButton.ToolTip = "پنهان کردن پنل نمادها";
            }
        }
    }
}