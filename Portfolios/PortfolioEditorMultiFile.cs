using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace TradeIt.Portfolios
{
    public partial class PortfolioEditorWindow
    {
        private void BrowseButton_MultiSelect_Click(object sender, RoutedEventArgs e)
        {
            if (FolderRadio.IsChecked == true)
            {
                var dialog = new Microsoft.Win32.OpenFolderDialog
                {
                    Title = "پوشه داده‌های بازار را انتخاب کنید"
                };

                if (dialog.ShowDialog() == true)
                    PathTextBox.Text = dialog.FolderName;

                return;
            }

            var fileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "انتخاب فایل‌های داده",
                Multiselect = true,
                Filter = "Data Files (*.txt;*.csv)|*.txt;*.csv|All Files (*.*)|*.*"
            };

            if (fileDialog.ShowDialog() != true)
                return;

            PathTextBox.Text = string.Join(";", fileDialog.FileNames);
        }
    }
}