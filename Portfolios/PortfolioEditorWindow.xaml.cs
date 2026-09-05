using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TradeIt.Models;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfComboBoxItem = System.Windows.Controls.ComboBoxItem;

namespace TradeIt.Portfolios
{
    public partial class PortfolioEditorWindow : Window
    {
        private bool _mappingLoaded;
        private DataTable? _previewTable;
        public Portfolio? ResultPortfolio { get; private set; }

        public PortfolioEditorWindow()
        {
            InitializeComponent();
            InitializeSymbolSelection();
            UpdateDateTimeControls();
        }

        private void NoDateTimeCheckBox_Changed(object sender, RoutedEventArgs e) => UpdateDateTimeControls();

        private void UpdateDateTimeControls()
        {
            if (NoDateTimeCheckBox == null) return;
            bool enabled = NoDateTimeCheckBox.IsChecked != true;
            CalendarComboBox.IsEnabled = enabled;
            DateFormatComboBox.IsEnabled = enabled;
            TimeFormatComboBox.IsEnabled = enabled;
            DateColumnCombo.IsEnabled = enabled;
            TimeColumnCombo.IsEnabled = enabled;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "پوشه فایل‌های داده بازار را انتخاب کنید" };
            if (dialog.ShowDialog() == true)
            {
                PathTextBox.Text = dialog.FolderName;
                LoadPreviewFromCurrentFolder();
            }
        }

        private void LoadPreviewButton_Click(object sender, RoutedEventArgs e) => LoadPreviewFromCurrentFolder();

        private void LoadPreviewFromCurrentFolder()
        {
            try
            {
                string folder = PathTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(folder)) { System.Windows.MessageBox.Show("ابتدا مسیر پوشه داده را انتخاب کنید."); return; }
                if (!Directory.Exists(folder)) { System.Windows.MessageBox.Show("مسیر انتخاب‌شده یک پوشه معتبر نیست."); return; }

                string[] files = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(IsDataFile)
                    .OrderBy(x => Path.GetFileName(x), StringComparer.CurrentCultureIgnoreCase)
                    .ToArray();
                if (files.Length == 0)
                {
                    _mappingLoaded = false;
                    _previewTable = null;
                    PreviewDataGrid.ItemsSource = null;
                    _symbolSelectionItems.Clear();
                    UpdateSelectedSymbolsCount();
                    System.Windows.MessageBox.Show("در این پوشه فایل CSV یا TXT پیدا نشد.");
                    return;
                }

                // ممکن است اولین فایل از نظر نام خالی باشد. برای Mapping باید اولین فایل دارای محتوا انتخاب شود.
                string? sampleFile = files.FirstOrDefault(file =>
                {
                    try
                    {
                        return File.ReadLines(file).Any(line => !string.IsNullOrWhiteSpace(line));
                    }
                    catch
                    {
                        return false;
                    }
                });

                if (sampleFile == null)
                {
                    _mappingLoaded = false;
                    _previewTable = null;
                    PreviewDataGrid.ItemsSource = null;
                    _symbolSelectionItems.Clear();
                    UpdateSelectedSymbolsCount();
                    System.Windows.MessageBox.Show("فایل‌های CSV/TXT پیدا شدند، اما همه آن‌ها خالی هستند.");
                    return;
                }

                LoadMappingFromFile(sampleFile);
                PopulateSymbolSelectionList(files);
            }
            catch (Exception ex)
            {
                _mappingLoaded = false;
                PreviewDataGrid.ItemsSource = null;
                System.Windows.MessageBox.Show(ex.ToString(), "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static bool IsDataFile(string path)
        {
            string extension = Path.GetExtension(path);
            return string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase);
        }

        private void LoadMappingFromFile(string filePath)
        {
            string delimiter = GetSelectedDelimiter();
            string[] lines = File.ReadLines(filePath).Take(51).ToArray();
            if (lines.Length == 0) { _mappingLoaded = false; PreviewDataGrid.ItemsSource = null; System.Windows.MessageBox.Show("فایل نمونه خالی است."); return; }

            bool hasHeader = HeaderCheckBox.IsChecked == true;
            string[] headers;
            int startRow;
            if (hasHeader) { headers = SplitLine(lines[0], delimiter); startRow = 1; }
            else { headers = Enumerable.Range(1, SplitLine(lines[0], delimiter).Length).Select(x => $"Column {x}").ToArray(); startRow = 0; }

            BuildColumnCombos(headers);
            AutoDetectColumns(headers);

            _previewTable = new DataTable();
            foreach (string header in headers)
            {
                string safeHeader = string.IsNullOrWhiteSpace(header) ? "Column" : header.Trim();
                string original = safeHeader;
                int counter = 2;
                while (_previewTable.Columns.Contains(safeHeader)) safeHeader = $"{original}_{counter++}";
                _previewTable.Columns.Add(safeHeader);
            }
            for (int i = startRow; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                string[] values = SplitLine(lines[i], delimiter);
                DataRow row = _previewTable.NewRow();
                for (int c = 0; c < _previewTable.Columns.Count; c++) row[c] = c < values.Length ? values[c].Trim() : "";
                _previewTable.Rows.Add(row);
            }
            PreviewDataGrid.ItemsSource = _previewTable.DefaultView;
            _mappingLoaded = headers.Length > 0;
        }

        private void BuildColumnCombos(string[] headers)
        {
            var combos = new WpfComboBox[] { SymbolColumnCombo, DateColumnCombo, TimeColumnCombo, OpenColumnCombo, HighColumnCombo, LowColumnCombo, CloseColumnCombo, VolumeColumnCombo, PreviousColumnCombo, ValueColumnCombo, TradeCountColumnCombo, EnglishTickerColumnCombo, ShareCountColumnCombo, MarketValueColumnCombo, TSECloseColumnCombo };
            foreach (WpfComboBox combo in combos)
            {
                combo.Items.Clear();
                combo.Items.Add(new ColumnOption { Index = -1, Name = "(None)" });
                for (int i = 0; i < headers.Length; i++) combo.Items.Add(new ColumnOption { Index = i, Name = $"{i + 1}: {headers[i]}" });
                combo.SelectedIndex = 0;
            }
        }

        private void AutoDetectColumns(string[] headers)
        {
            SelectColumn(SymbolColumnCombo, headers, "ticker", "symbol", "code", "namad");
            SelectColumn(DateColumnCombo, headers, "date"); SelectColumn(TimeColumnCombo, headers, "time");
            SelectColumn(OpenColumnCombo, headers, "open"); SelectColumn(HighColumnCombo, headers, "high"); SelectColumn(LowColumnCombo, headers, "low");
            SelectColumn(CloseColumnCombo, headers, "close", "last", "closingprice"); SelectColumn(VolumeColumnCombo, headers, "vol", "volume");
            SelectColumn(PreviousColumnCombo, headers, "previous", "prev", "yesterday"); SelectColumn(ValueColumnCombo, headers, "value", "tradevalue", "transactionvalue");
            SelectColumn(TradeCountColumnCombo, headers, "tradecount", "trades", "numberoftrades"); SelectColumn(EnglishTickerColumnCombo, headers, "englishticker", "englishsymbol", "en_symbol");
            SelectColumn(ShareCountColumnCombo, headers, "sharecount", "shares", "numberofshares"); SelectColumn(MarketValueColumnCombo, headers, "marketvalue", "marketcap", "marketcapitalization");
            SelectColumn(TSECloseColumnCombo, headers, "tseclose", "tse_close"); UpdateDateTimeControls();
        }

        private void SelectColumn(WpfComboBox combo, string[] headers, params string[] names)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                string header = headers[i].Trim().Trim('<', '>').ToLowerInvariant();
                foreach (string name in names) if (header == name || header.Contains(name)) { combo.SelectedIndex = i + 1; return; }
            }
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_mappingLoaded) { System.Windows.MessageBox.Show("ابتدا مسیر داده را انتخاب کنید."); return; }
            if (GetColumnIndex(OpenColumnCombo) < 0 || GetColumnIndex(HighColumnCombo) < 0 || GetColumnIndex(LowColumnCombo) < 0 || GetColumnIndex(CloseColumnCombo) < 0) { System.Windows.MessageBox.Show("ستون‌های OHLC باید مشخص شوند."); return; }
            if (SymbolFromFileContentRadio.IsChecked == true && GetColumnIndex(SymbolColumnCombo) < 0) { System.Windows.MessageBox.Show("منبع نام نماد روی «داخل فایل» است؛ بنابراین ستون Symbol باید مشخص شود."); return; }
            if (NoDateTimeCheckBox.IsChecked != true && (GetColumnIndex(DateColumnCombo) < 0 || GetColumnIndex(TimeColumnCombo) < 0)) { System.Windows.MessageBox.Show("وقتی داده دارای تاریخ/زمان است، ستون Date و Time باید مشخص شوند."); return; }
            System.Windows.MessageBox.Show("Mapping معتبر است.", "Test", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = PortfolioNameTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(name)) { System.Windows.MessageBox.Show("نام سبد را وارد کنید."); return; }
                string folder = PathTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) { System.Windows.MessageBox.Show("ابتدا مسیر پوشه داده را انتخاب کنید."); return; }
                if (!_mappingLoaded) { System.Windows.MessageBox.Show("ابتدا مسیر داده را انتخاب کنید تا فایل‌های آن خوانده شوند."); return; }

                int openColumn = GetColumnIndex(OpenColumnCombo), highColumn = GetColumnIndex(HighColumnCombo), lowColumn = GetColumnIndex(LowColumnCombo), closeColumn = GetColumnIndex(CloseColumnCombo);
                if (openColumn < 0 || highColumn < 0 || lowColumn < 0 || closeColumn < 0) { System.Windows.MessageBox.Show("ستون‌های OHLC باید مشخص شوند."); return; }
                bool hasDateTime = NoDateTimeCheckBox.IsChecked != true;
                int dateColumn = hasDateTime ? GetColumnIndex(DateColumnCombo) : -1, timeColumn = hasDateTime ? GetColumnIndex(TimeColumnCombo) : -1;
                if (hasDateTime && (dateColumn < 0 || timeColumn < 0)) { System.Windows.MessageBox.Show("ستون Date و Time باید مشخص شوند."); return; }
                if (_symbolSelectionItems.Count == 0) { System.Windows.MessageBox.Show("در مسیر انتخاب‌شده فایل داده‌ای برای انتخاب سهام وجود ندارد."); return; }

                var selected = _symbolSelectionItems.Where(x => x.IsSelected).Select(x => new SymbolInfo { Symbol = x.Symbol, DisplayName = x.Symbol, FilePath = x.FilePath }).ToList();
                if (selected.Count == 0) { System.Windows.MessageBox.Show("حداقل یک سهم را انتخاب کنید."); return; }

                bool symbolFromFile = SymbolFromFileContentRadio.IsChecked == true;
                int symbolColumn = symbolFromFile ? GetColumnIndex(SymbolColumnCombo) : -1;
                if (symbolFromFile && symbolColumn < 0) { System.Windows.MessageBox.Show("ستون Symbol مشخص نشده است."); return; }

                var portfolio = new Portfolio
                {
                    Name = name,
                    HigherTimeframeCapability = GetHigherTimeframeCapability(),
                    DataSource = new DataSource
                    {
                        SourceType = "Folder", Path = folder, Delimiter = GetSelectedDelimiter(), HasHeader = HeaderCheckBox.IsChecked == true,
                        SymbolSource = symbolFromFile ? "FileContent" : "FileName", DataType = "TseDaily", HasDateTime = hasDateTime,
                        Calendar = GetSelectedTag(CalendarComboBox, "Persian"), DateFormat = GetSelectedTag(DateFormatComboBox, "yyyyMMdd"), TimeFormat = GetSelectedTag(TimeFormatComboBox, "HHmmss"),
                        SymbolColumn = symbolColumn, DateColumn = dateColumn, TimeColumn = timeColumn, OpenColumn = openColumn, HighColumn = highColumn, LowColumn = lowColumn, CloseColumn = closeColumn,
                        VolumeColumn = GetColumnIndex(VolumeColumnCombo), TSECloseColumn = GetColumnIndex(TSECloseColumnCombo), PreviousColumn = GetColumnIndex(PreviousColumnCombo), ValueColumn = GetColumnIndex(ValueColumnCombo),
                        TradeCountColumn = GetColumnIndex(TradeCountColumnCombo), EnglishTickerColumn = GetColumnIndex(EnglishTickerColumnCombo), ShareCountColumn = GetColumnIndex(ShareCountColumnCombo), MarketValueColumn = GetColumnIndex(MarketValueColumnCombo)
                    },
                    UseExplicitSymbolList = true, Symbols = selected
                };

                // ذخیره در همان لحظه؛ فرم باز می‌ماند تا کاربر بتواند سبد دیگری بسازد.
                new PortfolioManager().Save(portfolio);
                ResultPortfolio = portfolio;

                if (Owner is TradeIt.MainWindow mainWindow)
                    mainWindow.RefreshPortfoliosAfterEditorSave(portfolio.Name);

                PortfolioNameTextBox.Clear();
                PortfolioNameTextBox.Focus();
            }
            catch (Exception ex) { System.Windows.MessageBox.Show(ex.ToString(), "خطا", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private HigherTimeframeCapability GetHigherTimeframeCapability()
        {
            if (HigherTimeframeCapabilityComboBox.SelectedItem is WpfComboBoxItem item &&
                Enum.TryParse<HigherTimeframeCapability>(item.Tag?.ToString(), out HigherTimeframeCapability capability))
                return capability;

            return HigherTimeframeCapability.None;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
        private string GetSelectedDelimiter() => DelimiterComboBox.SelectedItem is WpfComboBoxItem item ? item.Tag?.ToString() ?? "," : ",";
        private static string GetSelectedTag(WpfComboBox combo, string defaultValue) => combo.SelectedItem is WpfComboBoxItem item ? item.Tag?.ToString() ?? defaultValue : defaultValue;
        private static string[] SplitLine(string line, string delimiter) => line.Split(new[] { delimiter }, StringSplitOptions.None);
        private static int GetColumnIndex(WpfComboBox combo) => combo.SelectedItem is ColumnOption option ? option.Index : -1;

        private class ColumnOption { public int Index { get; set; } public string Name { get; set; } = ""; public override string ToString() => Name; }
    }
}
