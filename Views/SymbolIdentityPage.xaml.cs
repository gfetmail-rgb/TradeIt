using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TradeIt.Models;
using TradeIt.Services;

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
                if (_editingId != null && !string.Equals(_editingId, id, StringComparison.OrdinalIgnoreCase)) _store.Delete(_editingId);
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
            MessageBox.Show("قابلیت Import از Excel در مرحله بعد فعال خواهد شد. دکمه آن برای حفظ ساختار رابط کاربری باقی مانده است.", "Import Excel", MessageBoxButton.OK, MessageBoxImage.Information);
        }

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