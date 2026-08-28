using System;
using System.Windows;
using System.Windows.Controls;

namespace TradeIt.Charts
{
    public partial class ChartSettingsWindow : Window
    {
        public ChartSettings Settings { get; private set; }

        public ChartSettingsWindow(ChartSettings settings)
        {
            InitializeComponent();
            Settings = settings.Clone();
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

            foreach (ComboBoxItem item in LineWidthComboBox.Items)
            {
                if (double.TryParse(item.Tag?.ToString(), out double value) && Math.Abs(value - Settings.LineWidth) < 0.001)
                {
                    LineWidthComboBox.SelectedItem = item;
                    break;
                }
            }
            if (LineWidthComboBox.SelectedItem == null) LineWidthComboBox.SelectedIndex = 2;
        }

        private string? SelectColor(string currentColor)
        {
            try
            {
                var color = System.Drawing.ColorTranslator.FromHtml(currentColor);
                using var dialog = new System.Windows.Forms.ColorDialog
                {
                    Color = color,
                    FullOpen = true,
                    AnyColor = true
                };
                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return null;
                return $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
            }
            catch { return null; }
        }

        private void SetPreviewColor(System.Windows.Controls.Border preview, string color)
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
            if (LineWidthComboBox.SelectedItem is ComboBoxItem lineItem && double.TryParse(lineItem.Tag?.ToString(), out double lineWidth))
                Settings.LineWidth = lineWidth;

            if (GridPatternComboBox.SelectedItem is ComboBoxItem gridPattern)
                Settings.GridPattern = gridPattern.Tag?.ToString() ?? "Solid";

            if (GridLineWidthComboBox.SelectedItem is ComboBoxItem gridWidth && double.TryParse(gridWidth.Tag?.ToString(), out double gw))
                Settings.GridLineWidth = gw;

            if (_crosshairPatternComboBox?.SelectedItem is ComboBoxItem crossPattern)
                Settings.CrosshairPattern = crossPattern.Tag?.ToString() ?? "Dotted";

            if (_crosshairLineWidthComboBox?.SelectedItem is ComboBoxItem crossWidth && double.TryParse(crossWidth.Tag?.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double cw))
                Settings.CrosshairLineWidth = cw;

            Settings.CrosshairColor = _crosshairColor;
            Settings.OpenChartInNewTab = OpenChartInNewTabCheckBox.IsChecked == true;
            Settings.HasUserSavedSettings = true;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}