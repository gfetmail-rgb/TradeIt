using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;

namespace TradeIt.Portfolios
{
    public partial class PortfolioEditorWindow
    {
        private readonly ObservableCollection<SymbolSelectionItem> _symbolSelectionItems = new();
        private bool _symbolSelectionConfirmed;

        private void InitializeSymbolSelection()
        {
            SymbolSelectionGrid.ItemsSource = _symbolSelectionItems;
            UpdateSelectedSymbolsCount();
        }

        private void PopulateSymbolSelectionList()
        {
            _symbolSelectionItems.Clear();
            _symbolSelectionConfirmed = false;

            string[] symbols = GetSymbolsFromCurrentSource();
            foreach (string symbol in symbols)
            {
                _symbolSelectionItems.Add(new SymbolSelectionItem
                {
                    Symbol = symbol,
                    IsSelected = true
                });
            }

            UpdateSelectedSymbolsCount();
        }

        private string[] GetSymbolsFromCurrentSource()
        {
            if (SymbolFromFileContentRadio.IsChecked == true && _previewTable != null)
            {
                int symbolColumn = GetColumnIndex(SymbolColumnCombo);
                if (symbolColumn >= 0 && symbolColumn < _previewTable.Columns.Count)
                {
                    return _previewTable.Rows.Cast<DataRow>()
                        .Select(row => row[symbolColumn]?.ToString()?.Trim() ?? "")
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase)
                        .ToArray();
                }
            }

            string path = PathTextBox.Text.Trim();
            if (File.Exists(path))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                return string.IsNullOrWhiteSpace(name) ? Array.Empty<string>() : new[] { name };
            }

            return Array.Empty<string>();
        }

        private void SelectAllSymbolsButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (SymbolSelectionItem item in _symbolSelectionItems)
                item.IsSelected = true;

            _symbolSelectionConfirmed = false;
            SymbolSelectionGrid.Items.Refresh();
            UpdateSelectedSymbolsCount();
        }

        private void DeselectAllSymbolsButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (SymbolSelectionItem item in _symbolSelectionItems)
                item.IsSelected = false;

            _symbolSelectionConfirmed = false;
            SymbolSelectionGrid.Items.Refresh();
            UpdateSelectedSymbolsCount();
        }

        private void ConfirmSymbolSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedCount = _symbolSelectionItems.Count(x => x.IsSelected);
            if (selectedCount == 0)
            {
                System.Windows.MessageBox.Show(
                    "حداقل یک سهم را انتخاب کنید.",
                    "انتخاب سهام",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            _symbolSelectionConfirmed = true;
            UpdateSelectedSymbolsCount();

            System.Windows.MessageBox.Show(
                $"انتخاب {selectedCount} سهم تایید شد.",
                "انتخاب سهام",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void UpdateSelectedSymbolsCount()
        {
            if (SelectedSymbolsCountTextBlock == null) return;
            int selected = _symbolSelectionItems.Count(x => x.IsSelected);
            SelectedSymbolsCountTextBlock.Text = $"انتخاب شده: {selected} از {_symbolSelectionItems.Count}";
        }

        private bool TryApplySymbolSelection(Models.Portfolio portfolio)
        {
            if (FileRadio.IsChecked != true)
                return true;

            if (_symbolSelectionItems.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "برای منبع داده «فایل»، ابتدا فایل را بخوانید تا فهرست سهام نمایش داده شود.",
                    "انتخاب سهام",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            if (!_symbolSelectionConfirmed)
            {
                System.Windows.MessageBox.Show(
                    "ابتدا انتخاب‌های خود را با دکمه «تایید نهایی انتخاب‌ها» تایید کنید.",
                    "انتخاب سهام",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            var selected = _symbolSelectionItems
                .Where(x => x.IsSelected)
                .Select(x => new Models.SymbolInfo
                {
                    Symbol = x.Symbol,
                    DisplayName = x.Symbol,
                    FilePath = portfolio.DataSource.Path
                })
                .ToList();

            if (selected.Count == 0)
                return false;

            portfolio.UseExplicitSymbolList = true;
            portfolio.Symbols = selected;
            return true;
        }

        private sealed class SymbolSelectionItem : INotifyPropertyChanged
        {
            private bool _isSelected;

            public string Symbol { get; set; } = "";

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
