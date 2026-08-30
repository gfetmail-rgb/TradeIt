using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfButtonBase = System.Windows.Controls.Primitives.ButtonBase;

using TradeIt.Models;

namespace TradeIt.Portfolios
{
    public partial class PortfolioEditorWindow
    {
        private readonly List<string> _selectedFilePaths = new();

        // The DataType selector was intentionally removed from the UI.
        // Keep the historical default internally for existing persistence.
        private string DataTypeComboBox => "TseDaily";

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BrowseButton.AddHandler(
                UIElement.PreviewMouseLeftButtonDownEvent,
                new System.Windows.Input.MouseButtonEventHandler(BrowseMultiFilePreview),
                true);

            SaveButton.AddHandler(
                WpfButtonBase.ClickEvent,
                new RoutedEventHandler(SaveMultiFileClick),
                true);
        }

        private void BrowseMultiFilePreview(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (FileRadio.IsChecked != true)
                return;

            e.Handled = true;

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "انتخاب فایل‌های داده",
                Filter = "Data Files (*.txt;*.csv)|*.txt;*.csv|All Files (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() != true || dialog.FileNames.Length == 0)
                return;

            _selectedFilePaths.Clear();
            _selectedFilePaths.AddRange(
                dialog.FileNames
                    .Where(File.Exists)
                    .Distinct(StringComparer.OrdinalIgnoreCase));

            PathTextBox.Text = string.Join(Environment.NewLine, _selectedFilePaths);
        }

        private void SaveMultiFileClick(object sender, RoutedEventArgs e)
        {
            if (_selectedFilePaths.Count <= 1 || FileRadio.IsChecked != true)
                return;

            if (ResultPortfolio == null)
                return;

            string firstDirectory =
                Path.GetDirectoryName(_selectedFilePaths[0]) ?? "";

            ResultPortfolio.DataSource.SourceType = "Folder";
            ResultPortfolio.DataSource.Path = firstDirectory;
            ResultPortfolio.Symbols = _selectedFilePaths
                .Select(path => new SymbolInfo
                {
                    Symbol = Path.GetFileNameWithoutExtension(path),
                    DisplayName = Path.GetFileNameWithoutExtension(path),
                    FilePath = path,
                    IsSelected = false
                })
                .ToList();
            ResultPortfolio.UseExplicitSymbolList = true;
        }

        private static string GetSelectedTag(string value, string defaultValue)
        {
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }
    }
}
