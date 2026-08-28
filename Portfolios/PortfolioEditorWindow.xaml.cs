
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
        private DataTable? _previewTable;

        public Portfolio? ResultPortfolio { get; private set; }

        public PortfolioEditorWindow()
        {
            InitializeComponent();

            UpdateDateTimeControls();
        }

        // =========================================================
        // Source Type
        // =========================================================

        private void SourceTypeChanged(
            object sender,
            RoutedEventArgs e)
        {
            if (BrowseButton == null)
                return;

            BrowseButton.Content =
                FolderRadio.IsChecked == true
                    ? "انتخاب پوشه..."
                    : "انتخاب فایل...";
        }

        // =========================================================
        // Date / Time
        // =========================================================

        private void NoDateTimeCheckBox_Changed(
            object sender,
            RoutedEventArgs e)
        {
            UpdateDateTimeControls();
        }

        private void UpdateDateTimeControls()
        {
            if (NoDateTimeCheckBox == null)
                return;

            bool enabled =
                NoDateTimeCheckBox.IsChecked != true;

            CalendarComboBox.IsEnabled = enabled;
            DateFormatComboBox.IsEnabled = enabled;
            TimeFormatComboBox.IsEnabled = enabled;

            DateColumnCombo.IsEnabled = enabled;
            TimeColumnCombo.IsEnabled = enabled;
        }

        // =========================================================
        // Browse
        // =========================================================

        private void BrowseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (FolderRadio.IsChecked == true)
            {
                var dialog =
                    new Microsoft.Win32.OpenFolderDialog
                    {
                        Title =
                            "پوشه داده‌های بازار را انتخاب کنید"
                    };

                if (dialog.ShowDialog() == true)
                {
                    PathTextBox.Text =
                        dialog.FolderName;
                }
            }
            else
            {
                var dialog =
                    new Microsoft.Win32.OpenFileDialog
                    {
                        Title =
                            "انتخاب فایل داده",

                        Filter =
                            "Data Files (*.txt;*.csv)|*.txt;*.csv|" +
                            "All Files (*.*)|*.*"
                    };

                if (dialog.ShowDialog() == true)
                {
                    PathTextBox.Text =
                        dialog.FileName;
                }
            }
        }

        // =========================================================
        // Load Preview
        // =========================================================

        private void LoadPreviewButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                string path =
                    PathTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(path))
                {
                    System.Windows.MessageBox.Show(
                        "ابتدا فایل یا پوشه را انتخاب کنید.");

                    return;
                }

                string filePath = path;

                if (Directory.Exists(path))
                {
                    string[] files =
                        Directory.GetFiles(
                            path,
                            "*.*",
                            SearchOption.TopDirectoryOnly)
                        .Where(x =>
                            string.Equals(
                                Path.GetExtension(x),
                                ".csv",
                                StringComparison.OrdinalIgnoreCase)
                            ||
                            string.Equals(
                                Path.GetExtension(x),
                                ".txt",
                                StringComparison.OrdinalIgnoreCase))
                        .ToArray();

                    if (files.Length == 0)
                    {
                        System.Windows.MessageBox.Show(
                            "در این پوشه فایل CSV یا TXT پیدا نشد.");

                        return;
                    }

                    filePath = files[0];
                }

                if (!File.Exists(filePath))
                {
                    System.Windows.MessageBox.Show(
                        "فایل پیدا نشد.");

                    return;
                }

                string delimiter =
                    GetSelectedDelimiter();

                string[] lines =
                    File.ReadLines(filePath)
                        .Take(100)
                        .ToArray();

                if (lines.Length == 0)
                {
                    System.Windows.MessageBox.Show(
                        "فایل خالی است.");

                    return;
                }

                bool hasHeader =
                    HeaderCheckBox.IsChecked == true;

                string[] headers;

                int startRow;

                if (hasHeader)
                {
                    headers =
                        SplitLine(
                            lines[0],
                            delimiter);

                    startRow = 1;
                }
                else
                {
                    string[] firstRow =
                        SplitLine(
                            lines[0],
                            delimiter);

                    headers =
                        Enumerable.Range(
                            1,
                            firstRow.Length)
                        .Select(x => $"Column {x}")
                        .ToArray();

                    startRow = 0;
                }

                BuildColumnCombos(headers);

                _previewTable =
                    new DataTable();

                foreach (string header in headers)
                {
                    string safeHeader =
                        string.IsNullOrWhiteSpace(header)
                            ? "Column"
                            : header.Trim();

                    string original =
                        safeHeader;

                    int counter = 2;

                    while (_previewTable.Columns
                        .Contains(safeHeader))
                    {
                        safeHeader =
                            $"{original}_{counter}";

                        counter++;
                    }

                    _previewTable.Columns.Add(
                        safeHeader);
                }

                for (int i = startRow;
                     i < lines.Length;
                     i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                        continue;

                    string[] values =
                        SplitLine(
                            lines[i],
                            delimiter);

                    DataRow row =
                        _previewTable.NewRow();

                    for (int c = 0;
                         c < _previewTable.Columns.Count;
                         c++)
                    {
                        row[c] =
                            c < values.Length
                                ? values[c].Trim()
                                : "";
                    }

                    _previewTable.Rows.Add(row);
                }

                PreviewGrid.ItemsSource =
                    _previewTable.DefaultView;

                AutoDetectColumns(headers);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    ex.ToString(),
                    "خطا",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // =========================================================
        // Column Combos
        // =========================================================

        private void BuildColumnCombos(
            string[] headers)
        {
            var combos =
                new WpfComboBox[]
                {
                    SymbolColumnCombo,
                    DateColumnCombo,
                    TimeColumnCombo,
                    OpenColumnCombo,
                    HighColumnCombo,
                    LowColumnCombo,
                    CloseColumnCombo,
                    VolumeColumnCombo,
                    PreviousColumnCombo,
                    ValueColumnCombo,
                    TradeCountColumnCombo,
                    EnglishTickerColumnCombo,
                    ShareCountColumnCombo,
                    MarketValueColumnCombo,
                    TSECloseColumnCombo
                };

            foreach (WpfComboBox combo in combos)
            {
                combo.Items.Clear();

                combo.Items.Add(
                    new ColumnOption
                    {
                        Index = -1,
                        Name = "(None)"
                    });

                for (int i = 0;
                     i < headers.Length;
                     i++)
                {
                    combo.Items.Add(
                        new ColumnOption
                        {
                            Index = i,
                            Name =
                                $"{i + 1}: {headers[i]}"
                        });
                }

                combo.SelectedIndex = 0;
            }
        }

        // =========================================================
        // Automatic Mapping
        // =========================================================

        private void AutoDetectColumns(
            string[] headers)
        {
            SelectColumn(
                SymbolColumnCombo,
                headers,
                "ticker",
                "symbol",
                "code",
                "namad");

            SelectColumn(
                DateColumnCombo,
                headers,
                "date");

            SelectColumn(
                TimeColumnCombo,
                headers,
                "time");

            SelectColumn(
                OpenColumnCombo,
                headers,
                "open");

            SelectColumn(
                HighColumnCombo,
                headers,
                "high");

            SelectColumn(
                LowColumnCombo,
                headers,
                "low");

            SelectColumn(
                CloseColumnCombo,
                headers,
                "close",
                "last",
                "closingprice");

            SelectColumn(
                VolumeColumnCombo,
                headers,
                "vol",
                "volume");

            SelectColumn(
                PreviousColumnCombo,
                headers,
                "previous",
                "prev",
                "yesterday");

            SelectColumn(
                ValueColumnCombo,
                headers,
                "value",
                "tradevalue",
                "transactionvalue");

            SelectColumn(
                TradeCountColumnCombo,
                headers,
                "tradecount",
                "trades",
                "numberoftrades");

            SelectColumn(
                EnglishTickerColumnCombo,
                headers,
                "englishticker",
                "englishsymbol",
                "en_symbol");

            SelectColumn(
                ShareCountColumnCombo,
                headers,
                "sharecount",
                "shares",
                "numberofshares");

            SelectColumn(
                MarketValueColumnCombo,
                headers,
                "marketvalue",
                "marketcap",
                "marketcapitalization");

            SelectColumn(
                TSECloseColumnCombo,
                headers,
                "tseclose",
                "tse_close");

            UpdateDateTimeControls();
        }

        private void SelectColumn(
            WpfComboBox combo,
            string[] headers,
            params string[] names)
        {
            for (int i = 0;
                 i < headers.Length;
                 i++)
            {
                string header =
                    headers[i]
                        .Trim()
                        .Trim('<', '>')
                        .ToLowerInvariant();

                foreach (string name in names)
                {
                    if (header == name ||
                        header.Contains(name))
                    {
                        combo.SelectedIndex =
                            i + 1;

                        return;
                    }
                }
            }
        }

        // =========================================================
        // Test
        // =========================================================

        private void TestButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_previewTable == null)
            {
                System.Windows.MessageBox.Show(
                    "ابتدا فایل را بخوانید.");

                return;
            }

            if (GetColumnIndex(
                    OpenColumnCombo) < 0 ||
                GetColumnIndex(
                    HighColumnCombo) < 0 ||
                GetColumnIndex(
                    LowColumnCombo) < 0 ||
                GetColumnIndex(
                    CloseColumnCombo) < 0)
            {
                System.Windows.MessageBox.Show(
                    "ستون‌های OHLC باید مشخص شوند.");

                return;
            }

            bool symbolFromFile =
                SymbolFromFileContentRadio.IsChecked == true;

            if (symbolFromFile &&
                GetColumnIndex(
                    SymbolColumnCombo) < 0)
            {
                System.Windows.MessageBox.Show(
                    "منبع نام نماد روی «داخل فایل» است؛ " +
                    "بنابراین ستون Symbol باید مشخص شود.");

                return;
            }

            if (NoDateTimeCheckBox.IsChecked != true)
            {
                if (GetColumnIndex(
                        DateColumnCombo) < 0 ||
                    GetColumnIndex(
                        TimeColumnCombo) < 0)
                {
                    System.Windows.MessageBox.Show(
                        "وقتی داده دارای تاریخ/زمان است، " +
                        "ستون Date و Time باید مشخص شوند.");

                    return;
                }
            }

            System.Windows.MessageBox.Show(
                "Mapping معتبر است.",
                "Test",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // =========================================================
        // Save
        // =========================================================

        private void SaveButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                string name =
                    PortfolioNameTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    System.Windows.MessageBox.Show(
                        "نام سبد را وارد کنید.");

                    return;
                }

                string path =
                    PathTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(path))
                {
                    System.Windows.MessageBox.Show(
                        "منبع داده را انتخاب کنید.");

                    return;
                }

                int openColumn =
                    GetColumnIndex(
                        OpenColumnCombo);

                int highColumn =
                    GetColumnIndex(
                        HighColumnCombo);

                int lowColumn =
                    GetColumnIndex(
                        LowColumnCombo);

                int closeColumn =
                    GetColumnIndex(
                        CloseColumnCombo);

                if (openColumn < 0 ||
                    highColumn < 0 ||
                    lowColumn < 0 ||
                    closeColumn < 0)
                {
                    System.Windows.MessageBox.Show(
                        "ستون‌های OHLC باید مشخص شوند.");

                    return;
                }

                bool symbolFromFile =
                    SymbolFromFileContentRadio.IsChecked == true;

                int symbolColumn = -1;

                if (symbolFromFile)
                {
                    symbolColumn =
                        GetColumnIndex(
                            SymbolColumnCombo);

                    if (symbolColumn < 0)
                    {
                        System.Windows.MessageBox.Show(
                            "ستون Symbol مشخص نشده است.");

                        return;
                    }
                }

                bool hasDateTime =
                    NoDateTimeCheckBox.IsChecked != true;

                int dateColumn = -1;
                int timeColumn = -1;

                if (hasDateTime)
                {
                    dateColumn =
                        GetColumnIndex(
                            DateColumnCombo);

                    timeColumn =
                        GetColumnIndex(
                            TimeColumnCombo);

                    if (dateColumn < 0 ||
                        timeColumn < 0)
                    {
                        System.Windows.MessageBox.Show(
                            "ستون Date و Time باید مشخص شوند.");

                        return;
                    }
                }

                string calendar =
                    GetSelectedTag(
                        CalendarComboBox,
                        "Persian");

                string dateFormat =
                    GetSelectedTag(
                        DateFormatComboBox,
                        "yyyyMMdd");

                string timeFormat =
                    GetSelectedTag(
                        TimeFormatComboBox,
                        "HHmmss");

                string dataType =
                    GetSelectedTag(
                        DataTypeComboBox,
                        "TseDaily");

                var portfolio =
                    new Portfolio
                    {
                        Name = name,

                        DataSource =
                            new DataSource
                            {
                                SourceType =
                                    FolderRadio.IsChecked == true
                                        ? "Folder"
                                        : "File",

                                Path = path,

                                Delimiter =
                                    GetSelectedDelimiter(),

                                HasHeader =
                                    HeaderCheckBox.IsChecked == true,

                                SymbolSource =
                                    symbolFromFile
                                        ? "FileContent"
                                        : "FileName",

                                DataType =
                                    dataType,

                                HasDateTime =
                                    hasDateTime,

                                Calendar =
                                    calendar,

                                DateFormat =
                                    dateFormat,

                                TimeFormat =
                                    timeFormat,

                                SymbolColumn =
                                    symbolColumn,

                                DateColumn =
                                    dateColumn,

                                TimeColumn =
                                    timeColumn,

                                OpenColumn =
                                    openColumn,

                                HighColumn =
                                    highColumn,

                                LowColumn =
                                    lowColumn,

                                CloseColumn =
                                    closeColumn,

                                VolumeColumn =
                                    GetColumnIndex(
                                        VolumeColumnCombo),

                                TSECloseColumn =
                                    GetColumnIndex(
                                        TSECloseColumnCombo),

                                PreviousColumn =
                                    GetColumnIndex(
                                        PreviousColumnCombo),

                                ValueColumn =
                                    GetColumnIndex(
                                        ValueColumnCombo),

                                TradeCountColumn =
                                    GetColumnIndex(
                                        TradeCountColumnCombo),

                                EnglishTickerColumn =
                                    GetColumnIndex(
                                        EnglishTickerColumnCombo),

                                ShareCountColumn =
                                    GetColumnIndex(
                                        ShareCountColumnCombo),

                                MarketValueColumn =
                                    GetColumnIndex(
                                        MarketValueColumnCombo)
                            }
                    };

                ResultPortfolio =
                    portfolio;

                DialogResult = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    ex.ToString(),
                    "خطا",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // =========================================================
        // Cancel
        // =========================================================

        private void CancelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = false;
        }

        // =========================================================
        // Helpers
        // =========================================================

        private string GetSelectedDelimiter()
        {
            if (DelimiterComboBox.SelectedItem
                is WpfComboBoxItem item)
            {
                return item.Tag?.ToString() ?? ",";
            }

            return ",";
        }

        private static string GetSelectedTag(
            WpfComboBox combo,
            string defaultValue)
        {
            if (combo.SelectedItem
                is WpfComboBoxItem item)
            {
                return item.Tag?.ToString()
                    ?? defaultValue;
            }

            return defaultValue;
        }

        private static string[] SplitLine(
            string line,
            string delimiter)
        {
            return line.Split(
                new[] { delimiter },
                StringSplitOptions.None);
        }

        private static int GetColumnIndex(
            WpfComboBox combo)
        {
            if (combo.SelectedItem
                is ColumnOption option)
            {
                return option.Index;
            }

            return -1;
        }

        private class ColumnOption
        {
            public int Index { get; set; }

            public string Name { get; set; } = "";

            public override string ToString()
            {
                return Name;
            }
        }
    }
}

