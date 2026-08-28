using System;
using System.IO;
using System.Linq;
using System.Windows;
using TradeIt.Models;

namespace TradeIt.Portfolios
{
    public partial class PortfolioEditorWindow
    {
        private void LoadPreviewButton_MultiFile_Click(object sender, RoutedEventArgs e)
        {
            string path = PathTextBox.Text.Trim();

            if (Directory.Exists(path))
            {
                LoadPreviewButton_Click(sender, e);
                return;
            }

            string[] files = GetSelectedFilesForMultiFile();
            if (files.Length == 0)
            {
                System.Windows.MessageBox.Show(
                    "ابتدا یک فایل یا پوشه داده را انتخاب کنید.",
                    "پیش‌نمایش",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string original = PathTextBox.Text;
            PathTextBox.Text = files[0];
            try
            {
                LoadPreviewButton_Click(sender, e);
            }
            finally
            {
                PathTextBox.Text = original;
            }
        }

        private void SaveButton_MultiFile_Click(object sender, RoutedEventArgs e)
        {
            string path = PathTextBox.Text.Trim();

            if (Directory.Exists(path))
            {
                SaveButton_Click(sender, e);
                return;
            }

            string[] selectedFiles = GetSelectedFilesForMultiFile();

            if (selectedFiles.Length <= 1)
            {
                SaveButton_Click(sender, e);
                return;
            }

            string folder = Path.GetDirectoryName(selectedFiles[0]) ?? "";
            if (selectedFiles.Any(x => !string.Equals(
                    Path.GetDirectoryName(x),
                    folder,
                    StringComparison.OrdinalIgnoreCase)))
            {
                System.Windows.MessageBox.Show(
                    this,
                    "برای انتخاب چند فایل، فایل‌ها باید در یک پوشه باشند.",
                    "منبع داده",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string originalPath = PathTextBox.Text;
            PathTextBox.Text = folder;
            try
            {
                SaveButton_Click(sender, e);

                if (ResultPortfolio != null)
                {
                    ResultPortfolio.DataSource.SourceType = "Folder";
                    ResultPortfolio.DataSource.Path = folder;
                    ResultPortfolio.UseExplicitSymbolList = true;
                    ResultPortfolio.Symbols = selectedFiles.Select(file => new SymbolInfo
                    {
                        Symbol = Path.GetFileNameWithoutExtension(file),
                        DisplayName = Path.GetFileNameWithoutExtension(file),
                        FilePath = file
                    }).ToList();
                }
            }
            finally
            {
                PathTextBox.Text = originalPath;
            }
        }

        private string[] GetSelectedFilesForMultiFile()
        {
            return PathTextBox.Text
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(File.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
