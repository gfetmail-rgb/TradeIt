using System.Windows;

namespace TradeIt
{
    public partial class MainWindow
    {
        private bool _symbolsPanelCollapsed;
        private bool _symbolTableGroupCollapsed;
        private bool _symbolFilterGroupCollapsed;
        private bool _symbolOperationsGroupCollapsed;

        private void SymbolTableCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            _symbolTableGroupCollapsed = !_symbolTableGroupCollapsed;

            // Keep the group header visible so the user can always restore the table.
            SymbolsDataGrid.Visibility = _symbolTableGroupCollapsed ? Visibility.Collapsed : Visibility.Visible;
            SymbolTableCollapseButton.Content = _symbolTableGroupCollapsed ? "▼" : "▲";
        }

        private void SymbolFilterCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            _symbolFilterGroupCollapsed = !_symbolFilterGroupCollapsed;

            // Keep the group header visible so the user can always restore the filters.
            SymbolFilterHost.Visibility = _symbolFilterGroupCollapsed ? Visibility.Collapsed : Visibility.Visible;
            SymbolFilterCollapseButton.Content = _symbolFilterGroupCollapsed ? "▲" : "▼";
        }

        private void SymbolOperationsCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            _symbolOperationsGroupCollapsed = !_symbolOperationsGroupCollapsed;

            // Keep the group header visible so the user can always restore the operations.
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