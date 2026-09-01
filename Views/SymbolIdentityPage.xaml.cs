using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TradeIt.Models;
using TradeIt.Services;
using ClosedXML.Excel;

namespace TradeIt.Views
{
    public partial class SymbolIdentityPage : System.Windows.Controls.UserControl
    {
        private readonly SymbolIdentityStore _store = new();
        private string? _editingId;

        public SymbolIdentityPage()
        {
            InitializeComponent();
            LoadItems();
            ClearEditor();
        }

        private void LoadItems()
        {
            IdentitiesDataGrid.ItemsSource = null;
            IdentitiesDataGrid.ItemsSource = _store.GetAll().OrderBy(x => x.SymbolNameFa).ToList();
            StatusTextBlock.Text = $"تعداد شناسه‌ها: {IdentitiesDataGrid.Items.Count:N0}";
        }

        private void NewButton_Click(object sender, RoutedEventArgs e) => ClearEditor();

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string id = SymbolId12TextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(id))
            {
                MessageBox.Show("کد 12 رقمی نماد الزامی است.", "شناسه", MessageBoxButton.OK, MessageBoxImage.Warning);
                SymbolId12TextBox.Focus();
                return;
            }

            try
            {
                if (_editingId != null && !string.Equals(_editingId, id, StringComparison.OrdinalIgnoreCase))
                    _store.Delete(_editingId);

                _store.Upsert(ReadEditor());
                LoadItems();
                SelectItem(id);
                StatusTextBlock.Text = "شناسه با موفقیت ذخیره شد.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطا در ذخیره شناسه", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = IdentitiesDataGrid.SelectedItems.Cast<SymbolIdentity>().ToList();
            if (selected.Count == 0) return;
            if (MessageBox.Show($"{selected.Count:N0} شناسه انتخاب‌شده حذف شود؟", "تأیید حذف", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            _store.DeleteMany(selected.Select(x => x.SymbolId12));
            LoadItems();
            ClearEditor();
        }

        private void DeleteAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (IdentitiesDataGrid.Items.Count == 0) return;
            if (MessageBox.Show("تمام اطلاعات شناسه‌ها حذف خواهد شد. این عملیات قابل برگشت نیست. ادامه می‌دهید؟", "حذف کامل", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            _store.DeleteAll();
            LoadItems();
            ClearEditor();
        }

        private void IdentitiesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IdentitiesDataGrid.SelectedItem is SymbolIdentity item) FillEditor(item);
        }

        private void ImportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Title = "انتخاب فایل Excel شناسه نمادها", Filter = "Excel (*.xlsx)|*.xlsx|همه فایل‌ها (*.*)|*.*", Multiselect = false };
            if (dialog.ShowDialog() != true) return;
            try
            {
                List<SymbolIdentity> imported = ReadExcel(dialog.FileName);
                if (imported.Count == 0)
                {
                    MessageBox.Show("هیچ رکورد معتبر دارای کد 12 رقمی نماد در فایل پیدا نشد.", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                var existing = new HashSet<string>(_store.GetAll().Select(x => x.SymbolId12), StringComparer.OrdinalIgnoreCase);
                int newCount = imported.Count(x => !existing.Contains(x.SymbolId12));
                int duplicateCount = imported.Count - newCount;
                string message = $"تعداد رکوردهای قابل ورود: {imported.Count:N0}\nجدید: {newCount:N0}\nتکراری: {duplicateCount:N0}\n\nرکوردهای تکراری به‌روزرسانی می‌شوند. ادامه؟";
                if (MessageBox.Show(message, "پیش‌نمایش Import", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                foreach (var item in imported) _store.Upsert(item);
                LoadItems();
                StatusTextBlock.Text = $"Import انجام شد: {imported.Count:N0} رکورد.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "خطا در Import Excel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static List<SymbolIdentity> ReadExcel(string path)
        {
            using var workbook = new XLWorkbook(path);
            var ws = workbook.Worksheets.FirstOrDefault() ?? throw new InvalidDataException("فایل Excel فاقد Sheet است.");
            var used = ws.RangeUsed() ?? throw new InvalidDataException("Sheet فایل Excel خالی است.");
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in used.FirstRow().Cells())
            {
                string h = NormalizeHeader(cell.GetString());
                if (!string.IsNullOrWhiteSpace(h)) headers[h] = cell.Address.ColumnNumber;
            }
            string[][] aliases =
            {
                new[] { "کد12رقمی نماد", "کد 12 رقمی نماد", "SymbolId12", "SymbolId" }, new[] { "کد5رقمی نماد", "کد 5 رقمی نماد", "SymbolCode5", "SymbolCode" },
                new[] { "نام لاتین شرکت", "CompanyNameLatin" }, new[] { "کد4رقمی شرکت", "کد 4 رقمی شرکت", "CompanyCode4" }, new[] { "نام شرکت", "CompanyName" },
                new[] { "نماد فارسی", "SymbolNameFa", "نماد" }, new[] { "نماد30رقمی فارسی", "نماد 30 رقمی فارسی", "SymbolName30Fa" }, new[] { "کد12رقمی شرکت", "کد 12 رقمی شرکت", "CompanyId12" },
                new[] { "بازار", "Market" }, new[] { "کد تابلو", "BoardCode" }, new[] { "کد گروه صنعت", "IndustryCode" }, new[] { "گروه صنعت", "IndustryName" },
                new[] { "کد زیرگروه صنعت", "کد زیر گروه صنعت", "SubIndustryCode" }, new[] { "زیرگروه صنعت", "زیر گروه صنعت", "SubIndustryName" }
            };
            int[] col = aliases.Select(a => FindColumn(headers, a)).ToArray();
            if (col[0] <= 0) throw new InvalidDataException("ستون «کد 12 رقمی نماد» در فایل Excel پیدا نشد.");
            var result = new List<SymbolIdentity>();
            foreach (var row in used.Rows().Skip(1))
            {
                string Value(int i) => i > 0 ? row.Cell(i).GetString().Trim() : "";
                string id = Value(col[0]);
                if (string.IsNullOrWhiteSpace(id)) continue;
                result.Add(new SymbolIdentity { SymbolId12 = id, SymbolCode5 = Value(col[1]), CompanyNameLatin = Value(col[2]), CompanyCode4 = Value(col[3]), CompanyName = Value(col[4]), SymbolNameFa = Value(col[5]), SymbolName30Fa = Value(col[6]), CompanyId12 = Value(col[7]), Market = Value(col[8]), BoardCode = Value(col[9]), IndustryCode = Value(col[10]), IndustryName = Value(col[11]), SubIndustryCode = Value(col[12]), SubIndustryName = Value(col[13]) });
            }
            return result;
        }

        private static int FindColumn(Dictionary<string, int> headers, IEnumerable<string> aliases)
        {
            foreach (string alias in aliases) if (headers.TryGetValue(NormalizeHeader(alias), out int c)) return c;
            return -1;
        }

        private static string NormalizeHeader(string value) => value.Replace("ي", "ی").Replace("ك", "ک").Replace("‌", "").Replace(" ", "").Replace("_", "").Trim();

        private SymbolIdentity ReadEditor() => new()
        {
            SymbolId12 = SymbolId12TextBox.Text.Trim(), SymbolCode5 = SymbolCode5TextBox.Text.Trim(), CompanyNameLatin = CompanyNameLatinTextBox.Text.Trim(), CompanyCode4 = CompanyCode4TextBox.Text.Trim(), CompanyName = CompanyNameTextBox.Text.Trim(), SymbolNameFa = SymbolNameFaTextBox.Text.Trim(), SymbolName30Fa = SymbolName30FaTextBox.Text.Trim(), CompanyId12 = CompanyId12TextBox.Text.Trim(), Market = MarketTextBox.Text.Trim(), BoardCode = BoardCodeTextBox.Text.Trim(), IndustryCode = IndustryCodeTextBox.Text.Trim(), IndustryName = IndustryNameTextBox.Text.Trim(), SubIndustryCode = SubIndustryCodeTextBox.Text.Trim(), SubIndustryName = SubIndustryNameTextBox.Text.Trim()
        };

        private void FillEditor(SymbolIdentity x)
        {
            _editingId = x.SymbolId12;
            SymbolId12TextBox.Text = x.SymbolId12; SymbolCode5TextBox.Text = x.SymbolCode5; CompanyNameLatinTextBox.Text = x.CompanyNameLatin; CompanyCode4TextBox.Text = x.CompanyCode4; CompanyNameTextBox.Text = x.CompanyName; SymbolNameFaTextBox.Text = x.SymbolNameFa; SymbolName30FaTextBox.Text = x.SymbolName30Fa; CompanyId12TextBox.Text = x.CompanyId12; MarketTextBox.Text = x.Market; BoardCodeTextBox.Text = x.BoardCode; IndustryCodeTextBox.Text = x.IndustryCode; IndustryNameTextBox.Text = x.IndustryName; SubIndustryCodeTextBox.Text = x.SubIndustryCode; SubIndustryNameTextBox.Text = x.SubIndustryName;
        }

        private void SelectItem(string id) => IdentitiesDataGrid.SelectedItem = IdentitiesDataGrid.Items.Cast<SymbolIdentity>().FirstOrDefault(x => string.Equals(x.SymbolId12, id, StringComparison.OrdinalIgnoreCase));

        private void ClearEditor()
        {
            _editingId = null;
            foreach (var box in new[] { SymbolId12TextBox, SymbolCode5TextBox, CompanyNameLatinTextBox, CompanyCode4TextBox, CompanyNameTextBox, SymbolNameFaTextBox, SymbolName30FaTextBox, CompanyId12TextBox, MarketTextBox, BoardCodeTextBox, IndustryCodeTextBox, IndustryNameTextBox, SubIndustryCodeTextBox, SubIndustryNameTextBox }) box.Clear();
            IdentitiesDataGrid.SelectedItem = null;
        }
    }
}