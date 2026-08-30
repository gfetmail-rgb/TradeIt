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
                if (double.TryParse(item.Tag?.ToString(), out double value) &&
                    Math.Abs(value - Settings.LineWidth) < 0.001)
                {
                    LineWidthComboBox.SelectedItem = item;
                    break;
                }
            }

            if (LineWidthComboBox.SelectedItem == null)
                LineWidthComboBox.SelectedIndex = 2;

            ShowNonTradingDaysCheckBox.IsChecked = Settings.ShowNonTradingDays;
            OpenSymbolsInSharedChartRadioButton.IsChecked = Settings.OpenSymbolsInSharedChart;
            OpenSymbolsInSeparateChartsRadioButton.IsChecked = !Settings.OpenSymbolsInSharedChart;
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

                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return null;

                return $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
            }
            catch
            {
                return null;
            }
        }

        private void SetPreviewColor(System.Windows.Controls.Border preview, string color)
        {
            try
            {
                var mediaColor = (System.Windows.Media.Color)
                    System.Windows.Media.ColorConverter.ConvertFromString(color);
                preview.Background = new System.Windows.Media.SolidColorBrush(mediaColor);
            }
            catch
            {
                preview.Background = System.Windows.Media.Brushes.White;
            }
        }

        private void RisingColorButton_Click(object sender, RoutedEventArgs e)
        {
            string? color = SelectColor(Settings.RisingColor);
            if (color == null) return;
            Settings.RisingColor = color;
            SetPreviewColor(RisingColorPreview, color);
        }

        private void FallingColorButton_Click(object sender, RoutedEventArgs e)
        {
            string? color = SelectColor(Settings.FallingColor);
            if (color == null) return;
            Settings.FallingColor = color;
            SetPreviewColor(FallingColorPreview, color);
        }

        private void LineColorButton_Click(object sender, RoutedEventArgs e)
        {
            string? color = SelectColor(Settings.LineColor);
            if (color == null) return;
            Settings.LineColor = color;
            SetPreviewColor(LineColorPreview, color);
        }

        private void FigureBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            string? color = SelectColor(Settings.FigureBackground);
            if (color == null) return;
            Settings.FigureBackground = color;
            SetPreviewColor(FigureBackgroundPreview, color);
        }

        private void DataBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            string? color = SelectColor(Settings.DataBackground);
            if (color == null) return;
            Settings.DataBackground = color;
            SetPreviewColor(DataBackgroundPreview, color);
        }

        private void AxisColorButton_Click(object sender, RoutedEventArgs e)
        {
            string? color = SelectColor(Settings.AxisColor);
            if (color == null) return;
            Settings.AxisColor = color;
            SetPreviewColor(AxisColorPreview, color);
        }

        private void GridColorButton_Click(object sender, RoutedEventArgs e)
        {
            string? color = SelectColor(Settings.GridColor);
            if (color == null) return;
            Settings.GridColor = color;
            SetPreviewColor(GridColorPreview, color);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (LineWidthComboBox.SelectedItem is ComboBoxItem item &&
                double.TryParse(item.Tag?.ToString(), out double width))
            {
                Settings.LineWidth = width;
            }

            Settings.ShowNonTradingDays =
                ShowNonTradingDaysCheckBox.IsChecked == true;

            Settings.OpenSymbolsInSharedChart =
                OpenSymbolsInSharedChartRadioButton.IsChecked == true;

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
