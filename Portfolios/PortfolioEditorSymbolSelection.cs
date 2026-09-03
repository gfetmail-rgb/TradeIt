using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace TradeIt.Portfolios
{
    public partial class PortfolioEditorWindow : Window
    {
        private readonly ObservableCollection<SymbolSelectionItem> _symbolSelectionItems = new();

        private void InitializeSymbolSelection()
        {
            SymbolSelectionGrid.ItemsSource = _symbolSelectionItems;
            UpdateSelectedSymbolsCount();
        }

        private void PopulateSymbolSelectionList(string[] files)
        {
            _symbolSelectionItems.Clear();

            foreach (string file in files)
            {
                string symbol = Path.GetFileNameWithoutExtension(file)?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(symbol))
                    continue;

                _symbolSelectionItems.Add(new SymbolSelectionItem
                {
                    Symbol = symbol,
                    FilePath = file,
                    IsSelected = true
                });
            }

            UpdateSelectedSymbolsCount();
        }

        private void SelectAllSymbolsButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (SymbolSelectionItem item in _symbolSelectionItems)
                item.IsSelected = true;

            SymbolSelectionGrid.Items.Refresh();
            UpdateSelectedSymbolsCount();
        }

        private void DeselectAllSymbolsButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (SymbolSelectionItem item in _symbolSelectionItems)
                item.IsSelected = false;

            SymbolSelectionGrid.Items.Refresh();
            UpdateSelectedSymbolsCount();
        }

        private void UpdateSelectedSymbolsCount()
        {
            if (SelectedSymbolsCountTextBlock == null) return;
            int selected = _symbolSelectionItems.Count(x => x.IsSelected);
            SelectedSymbolsCountTextBlock.Text = $"انتخاب شده: {selected} از {_symbolSelectionItems.Count}";
        }

        private sealed class SymbolSelectionItem
        {
            public string Symbol { get; set; } = "";
            public string FilePath { get; set; } = "";
            public bool IsSelected { get; set; }
        }
    }
}
