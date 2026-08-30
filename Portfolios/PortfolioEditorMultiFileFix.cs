using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using TradeIt.Models;

namespace TradeIt.Portfolios
{
    public partial class PortfolioEditorWindow
    {
        private readonly List<string> _selectedFilePaths = new();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BrowseButton.AddHandler(
                UIElement.PreviewMouseLeftButtonDownEvent,
                new System.Windows.Input.MouseButtonEventHandler(BrowseMultiFilePreview),
                true);

            PreviewGrid.AddHandler(
                ButtonBase.ClickEvent,
                new RoutedEventHandler(PreviewMultiFileClick),
                true);

            SaveButton.AddHandler(
                ButtonBase.ClickEvent,
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

        private void PreviewMultiFileClick(object sender, RoutedEventArgs e)
        {
            if (_selectedFilePaths.Count <= 1 || FileRadio.IsChecked != true)
                return;

            e.Handled = true;

            string original = PathTextBox.Text;
            try
            {
                PathTextBox.Text = _selectedFilePaths[0];
                LoadPreviewButton_Click(sender, new RoutedEventArgs());
            }
            finally
            {
                PathTextBox.Text = string.Join(Environment.NewLine, _selectedFilePaths);
            }
        }

        private void SaveMultiFileClick(object sender, RoutedEventArgs e)
        {
            if (_selectedFilePaths.Count <= 1 || FileRadio.IsChecked != true)
                return;

            // SaveButton_Click has already run. Replace its single-file result
            // with an explicit file list before the dialog closes completely.
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
    }
}
