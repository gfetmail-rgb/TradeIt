using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace TradeIt.Portfolios
{
    public partial class PortfolioEditorWindow
    {
        static PortfolioEditorWindow()
        {
            EventManager.RegisterClassHandler(typeof(PortfolioEditorWindow), Button.ClickEvent, new RoutedEventHandler(PortfolioEditor_ButtonClicked));
            EventManager.RegisterClassHandler(typeof(PortfolioEditorWindow), TextBox.TextChangedEvent, new TextChangedEventHandler(PortfolioEditor_PathChanged));
            EventManager.RegisterClassHandler(typeof(PortfolioEditorWindow), FrameworkElement.LoadedEvent, new RoutedEventHandler(PortfolioEditor_Loaded));
        }

        private static void PortfolioEditor_ButtonClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || Window.GetWindow(button) is not PortfolioEditorWindow window)
                return;

            if (button.Name == "BrowseButton")
            {
                if (window.FolderRadio.IsChecked == true)
                {
                    QueuePreview(window);
                    return;
                }

                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "انتخاب فایل‌های داده",
                    Multiselect = true,
                    Filter = "Data Files (*.txt;*.csv)|*.txt;*.csv|All Files (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    window.PathTextBox.Text = string.Join(";", dialog.FileNames);
                    QueuePreview(window);
                }

                e.Handled = true;
                return;
            }

            if (button.Name == "LoadPreviewButton")
            {
                string path = window.PathTextBox.Text.Trim();
                string[] files = GetSelectedFiles(window);

                if (files.Length <= 1)
                    return;

                e.Handled = true;
                string originalPath = path;
                window.PathTextBox.Text = files[0];
                window.LoadPreviewButton_Click(window, e);
                window.PathTextBox.Text = originalPath;
                return;
            }

            if (button.Content?.ToString() != "ذخیره سبد")
                return;

            string[] selectedFiles = GetSelectedFiles(window);
            if (selectedFiles.Length <= 1)
                return;

            string folder = Path.GetDirectoryName(selectedFiles[0]) ?? "";
            if (selectedFiles.Any(x => !string.Equals(Path.GetDirectoryName(x), folder, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show(window, "برای انتخاب چند فایل، فایل‌ها باید در یک پوشه باشند.", "منبع داده", MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Handled = true;
                return;
            }

            e.Handled = true;

            string original = window.PathTextBox.Text;
            window.PathTextBox.Text = folder;
            window.SaveButton_Click(window, e);

            if (window.ResultPortfolio != null)
            {
                window.ResultPortfolio.DataSource.SourceType = "Folder";
                window.ResultPortfolio.DataSource.Path = folder;
                window.ResultPortfolio.UseExplicitSymbolList = true;
                window.ResultPortfolio.Symbols = selectedFiles
                    .Select(file => new Models.SymbolInfo
                    {
                        Symbol = Path.GetFileNameWithoutExtension(file),
                        DisplayName = Path.GetFileNameWithoutExtension(file),
                        FilePath = file
                    })
                    .ToList();
            }

            window.PathTextBox.Text = original;
        }

        private static string[] GetSelectedFiles(PortfolioEditorWindow window)
        {
            return window.PathTextBox.Text
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(File.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static void PortfolioEditor_PathChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox || Window.GetWindow(textBox) is not PortfolioEditorWindow window)
                return;

            QueuePreview(window);
        }

        private static void PortfolioEditor_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not PortfolioEditorWindow window || window.DataTypeComboBox == null)
                return;

            window.DataTypeComboBox.Visibility = Visibility.Collapsed;

            if (window.DataTypeComboBox.Parent is Grid grid)
            {
                int column = Grid.GetColumn(window.DataTypeComboBox);
                if (column >= 0 && column < grid.ColumnDefinitions.Count)
                    grid.ColumnDefinitions[column].Width = new GridLength(0);
                if (column > 0 && column - 1 < grid.ColumnDefinitions.Count)
                    grid.ColumnDefinitions[column - 1].Width = new GridLength(0);

                foreach (TextBlock text in grid.Children.OfType<TextBlock>())
                {
                    if (text.Text == "داده:")
                        text.Visibility = Visibility.Collapsed;
                }
            }
        }

        private static void QueuePreview(PortfolioEditorWindow window)
        {
            window.Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(() =>
                {
                    string path = window.PathTextBox.Text.Trim();
                    string previewFile = path.Split(';')
                        .Select(x => x.Trim())
                        .FirstOrDefault(File.Exists) ?? "";

                    if (string.IsNullOrWhiteSpace(previewFile) && Directory.Exists(path))
                        previewFile = path;

                    if (string.IsNullOrWhiteSpace(previewFile))
                        return;

                    string original = window.PathTextBox.Text;
                    if (!File.Exists(original))
                        window.PathTextBox.Text = previewFile;

                    window.LoadPreviewButton_Click(window, new RoutedEventArgs());

                    if (!string.Equals(window.PathTextBox.Text, original, StringComparison.Ordinal))
                        window.PathTextBox.Text = original;
                }));
        }
    }
}