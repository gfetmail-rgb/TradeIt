using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace TradeIt.Portfolios
{
    public partial class PortfolioEditorWindow : Window
    {
        private readonly ObservableCollection<SymbolSelectionItem> _symbolSelectionItems = new();
        private ICollectionView? _symbolSelectionView;

        private void InitializeSymbolSelection()
        {
            SymbolSelectionGrid.ItemsSource = _symbolSelectionItems;
            _symbolSelectionView = CollectionViewSource.GetDefaultView(_symbolSelectionItems);
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

            ApplySymbolFilter();
            UpdateSelectedSymbolsCount();
        }

        private void SymbolFilterTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplySymbolFilter();
        }

        private void ApplySymbolFilter()
        {
            if (_symbolSelectionView == null)
                return;

            string filter = SymbolFilterTextBox?.Text?.Trim() ?? "";
            _symbolSelectionView.Filter = string.IsNullOrWhiteSpace(filter)
                ? null
                : item => item is SymbolSelectionItem symbolItem &&
                          symbolItem.Symbol.Contains(filter, System.StringComparison.CurrentCultureIgnoreCase);

            _symbolSelectionView.Refresh();
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

        private sealed class SymbolSelectionItem : INotifyPropertyChanged
        {
            private bool _isSelected;

            public string Symbol { get; set; } = "";
            public string FilePath { get; set; } = "";

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    if (_isSelected == value) return;
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
        }
    }
}
