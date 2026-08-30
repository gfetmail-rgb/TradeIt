using System.Windows;

namespace TradeIt
{
    public partial class MainWindow
    {
        private bool _symbolsPanelCollapsed;
        private bool _symbolTableGroupCollapsed;
        private bool _symbolFilterGroupCollapsed;
        private bool _symbolOperationsGroupCollapsed;

        private void SymbolsPanelCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            _symbolsPanelCollapsed = !_symbolsPanelCollapsed;
            SymbolsGroupsContainer.Visibility = _symbolsPanelCollapsed ? Visibility.Collapsed : Visibility.Visible;
            SymbolsPanelColumn.Width = _symbolsPanelCollapsed ? new GridLength(28) : new GridLength(300);
            SymbolsPanelCollapseButton.Content = _symbolsPanelCollapsed ? "›" : "‹";
        }

        private void SymbolTableCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            _symbolTableGroupCollapsed = !_symbolTableGroupCollapsed;
            SymbolTableGroupRow.Height = _symbolTableGroupCollapsed ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
            SymbolTableGroup.Visibility = _symbolTableGroupCollapsed ? Visibility.Collapsed : Visibility.Visible;
            SymbolTableCollapseButton.Content = _symbolTableGroupCollapsed ? "▼" : "▲";
        }

        private void SymbolFilterCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            _symbolFilterGroupCollapsed = !_symbolFilterGroupCollapsed;
            SymbolFilterGroupRow.Height = _symbolFilterGroupCollapsed ? new GridLength(0) : GridLength.Auto;
            SymbolFilterGroup.Visibility = _symbolFilterGroupCollapsed ? Visibility.Collapsed : Visibility.Visible;
            SymbolFilterCollapseButton.Content = _symbolFilterGroupCollapsed ? "▲" : "▼";
        }

        private void SymbolOperationsCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            _symbolOperationsGroupCollapsed = !_symbolOperationsGroupCollapsed;
            SymbolOperationsGroupRow.Height = _symbolOperationsGroupCollapsed ? new GridLength(0) : GridLength.Auto;
            SymbolOperationsGroup.Visibility = _symbolOperationsGroupCollapsed ? Visibility.Collapsed : Visibility.Visible;
            SymbolOperationsCollapseButton.Content = _symbolOperationsGroupCollapsed ? "▲" : "▼";
        }
    }
}