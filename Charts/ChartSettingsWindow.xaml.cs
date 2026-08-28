using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace TradeIt.Charts
{
    public partial class ChartSettingsWindow : Window
    {
        public ChartSettings Settings { get; private set; }
        private string _crosshairColor = "#909090";

        public ChartSettingsWindow(ChartSettings settings)
        {
            InitializeComponent();
            Settings = settings.Clone();
            _crosshairColor = Settings.CrosshairColor;
            LoadSettings();
        }

        private void LoadSettings()
        {
            SetPreviewColor(RisingColorPreview, Settings.RisingColor);
            SetPreviewColor(FallingColorPreview, Settings.FallingColor);
            SetPreviewColor(LineColorPreview, Settings.LineColor);
            SetPreviewColor(FigureBackgroundPreview, Settings.FigureBackground);
            SetPreviewColor(DataBackgroundPreview, Settings.DataBackground);
            SetPreviewColor(GridColorPreview, Settings.GridColor);
            SetPreviewColor(AxisColorPreview, Settings.AxisColor);
            SelectComboValue(LineWidthComboBox, Settings.LineWidth);
            SelectComboValue(CandleLineWidthComboBox, Settings.CandleLineWidth);
            SelectComboValue(BarLineWidthComboBox, Settings.BarLineWidth);
            SelectComboValue(GridLineWidthComboBox, Settings.GridLineWidth);
            SelectTag(GridPatternComboBox, Settings.GridPattern);
            OpenChartInNewTabCheckBox.IsChecked = Settings.OpenChartInNewTab;
        }

        private static void SelectComboValue(ComboBox combo, double value)
        {
            foreach (ComboBoxItem item in combo.Items)
                if (double.TryParse(item.Tag?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) && Math.Abs(parsed - value) < 0.001) { combo.SelectedItem = item; return; }
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        }

        private static void SelectTag(ComboBox combo, string? tag)
        {
            foreach (ComboBoxItem item in combo.Items)
                if (string.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase)) { combo.SelectedItem = item; return; }
        }

        private string? SelectColor(string currentColor)
        {
            try
            {
                var color = System.Drawing.ColorTranslator.FromHtml(currentColor);
                using var dialog = new System.Windows.Forms.ColorDialog { Color = color, FullOpen = true, AnyColor = true };
                return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}" : null;
            }
            catch { return null; }
        }

        private static void SetPreviewColor(System.Windows.Controls.Border preview, string color)
        {
            try
            {
                var mediaColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color);
                preview.Background = new System.Windows.Media.SolidColorBrush(mediaColor);
            }
            catch { preview.Background = System.Windows.Media.Brushes.White; }
        }

        private void RisingColorButton_Click(object sender, RoutedEventArgs e) => ChooseColor(c => Settings.RisingColor = c, Settings.RisingColor, RisingColorPreview);
        private void FallingColorButton_Click(object sender, RoutedEventArgs e) => ChooseColor(c => Settings.FallingColor = c, Settings.FallingColor, FallingColorPreview);
        private void LineColorButton_Click(object sender, RoutedEventArgs e) => ChooseColor(c => Settings.LineColor = c, Settings.LineColor, LineColorPreview);
        private void FigureBackgroundButton_Click(object sender, RoutedEventArgs e) => ChooseColor(c => Settings.FigureBackground = c, Settings.FigureBackground, FigureBackgroundPreview);
        private void DataBackgroundButton_Click(object sender, RoutedEventArgs e) => ChooseColor(c => Settings.DataBackground = c, Settings.DataBackground, DataBackgroundPreview);
        private void AxisColorButton_Click(object sender, RoutedEventArgs e) => ChooseColor(c => Settings.AxisColor = c, Settings.AxisColor, AxisColorPreview);
        private void GridColorButton_Click(object sender, RoutedEventArgs e) => ChooseColor(c => Settings.GridColor = c, Settings.GridColor, GridColorPreview);

        private void ChooseColor(Action<string> assign, string current, System.Windows.Controls.Border preview)
        {
            string? color = SelectColor(current);
            if (color == null) return;
            assign(color);
            SetPreviewColor(preview, color);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.LineWidth = ReadComboValue(LineWidthComboBox, Settings.LineWidth);
            Settings.CandleLineWidth = ReadComboValue(CandleLineWidthComboBox, Settings.CandleLineWidth);
            Settings.BarLineWidth = ReadComboValue(BarLineWidthComboBox, Settings.BarLineWidth);
            Settings.GridLineWidth = ReadComboValue(GridLineWidthComboBox, Settings.GridLineWidth);
            if (GridPatternComboBox.SelectedItem is ComboBoxItem gridPattern) Settings.GridPattern = gridPattern.Tag?.ToString() ?? "Solid";
            if (_crosshairPatternComboBox?.SelectedItem is ComboBoxItem crossPattern) Settings.CrosshairPattern = crossPattern.Tag?.ToString() ?? "Dotted";
            if (_crosshairLineWidthComboBox?.SelectedItem is ComboBoxItem crossWidth && double.TryParse(crossWidth.Tag?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double cw)) Settings.CrosshairLineWidth = cw;
            Settings.CrosshairColor = _crosshairColor;
            Settings.OpenChartInNewTab = OpenChartInNewTabCheckBox.IsChecked == true;
            Settings.HasUserSavedSettings = true;
            ChartSettingsManager.Save(Settings);
            DialogResult = true;
        }

        private static double ReadComboValue(ComboBox combo, double fallback) => combo.SelectedItem is ComboBoxItem item && double.TryParse(item.Tag?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : fallback;
        private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
