using System;
using System.Windows;
using System.Windows.Controls;

namespace TradeIt.Charts
{
    public partial class ChartSettingsWindow : Window
    {
        public ChartSettings Settings { get; private set; }


        // =========================================================
        // Constructor
        // =========================================================

        public ChartSettingsWindow(
            ChartSettings settings)
        {
            InitializeComponent();

            // حتماً یک کپی مستقل ایجاد می‌کنیم.
            // تغییرات پنجره تنظیمات مستقیماً روی Chart اصلی
            // اعمال نمی‌شود.
            Settings =
                settings.Clone();

            LoadSettings();
        }


        // =========================================================
        // Load
        // =========================================================

        private void LoadSettings()
        {
            SetPreviewColor(
                RisingColorPreview,
                Settings.RisingColor);

            SetPreviewColor(
                FallingColorPreview,
                Settings.FallingColor);

            SetPreviewColor(
                LineColorPreview,
                Settings.LineColor);

            SetPreviewColor(
                FigureBackgroundPreview,
                Settings.FigureBackground);

            SetPreviewColor(
                DataBackgroundPreview,
                Settings.DataBackground);

            SetPreviewColor(
                GridColorPreview,
                Settings.GridColor);

            SetPreviewColor(
                AxisColorPreview,
                Settings.AxisColor);


            foreach (ComboBoxItem item
                     in LineWidthComboBox.Items)
            {
                if (double.TryParse(
                        item.Tag?.ToString(),
                        out double value))
                {
                    if (Math.Abs(
                            value -
                            Settings.LineWidth)
                        < 0.001)
                    {
                        LineWidthComboBox.SelectedItem =
                            item;

                        break;
                    }
                }
            }


            if (LineWidthComboBox.SelectedItem == null)
            {
                LineWidthComboBox.SelectedIndex = 2;
            }
        }


        // =========================================================
        // Color Dialog
        // =========================================================

        private string? SelectColor(
            string currentColor)
        {
            try
            {
                var color =
                    System.Drawing.ColorTranslator
                        .FromHtml(currentColor);


                using var dialog =
                    new System.Windows.Forms.ColorDialog
                    {
                        Color = color,
                        FullOpen = true,
                        AnyColor = true
                    };


                if (dialog.ShowDialog() !=
                    System.Windows.Forms.DialogResult.OK)
                {
                    return null;
                }


                return
                    $"#{dialog.Color.R:X2}" +
                    $"{dialog.Color.G:X2}" +
                    $"{dialog.Color.B:X2}";
            }
            catch
            {
                return null;
            }
        }


        // =========================================================
        // Preview
        // =========================================================

        private void SetPreviewColor(
            System.Windows.Controls.Border preview,
            string color)
        {
            try
            {
                var mediaColor =
                    (System.Windows.Media.Color)
                    System.Windows.Media.ColorConverter
                        .ConvertFromString(color);

                preview.Background =
                    new System.Windows.Media.SolidColorBrush(
                        mediaColor);
            }
            catch
            {
                preview.Background =
                    System.Windows.Media.Brushes.White;
            }
        }


        // =========================================================
        // Rising Color
        // =========================================================

        private void RisingColorButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string? color =
                SelectColor(
                    Settings.RisingColor);

            if (color == null)
                return;

            Settings.RisingColor =
                color;

            SetPreviewColor(
                RisingColorPreview,
                color);
        }


        // =========================================================
        // Falling Color
        // =========================================================

        private void FallingColorButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string? color =
                SelectColor(
                    Settings.FallingColor);

            if (color == null)
                return;

            Settings.FallingColor =
                color;

            SetPreviewColor(
                FallingColorPreview,
                color);
        }


        // =========================================================
        // Line Color
        // =========================================================

        private void LineColorButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string? color =
                SelectColor(
                    Settings.LineColor);

            if (color == null)
                return;

            Settings.LineColor =
                color;

            SetPreviewColor(
                LineColorPreview,
                color);
        }


        // =========================================================
        // Figure Background
        // =========================================================

        private void FigureBackgroundButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string? color =
                SelectColor(
                    Settings.FigureBackground);

            if (color == null)
                return;

            Settings.FigureBackground =
                color;

            SetPreviewColor(
                FigureBackgroundPreview,
                color);
        }


        // =========================================================
        // Data Background
        // =========================================================

        private void DataBackgroundButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string? color =
                SelectColor(
                    Settings.DataBackground);

            if (color == null)
                return;

            Settings.DataBackground =
                color;

            SetPreviewColor(
                DataBackgroundPreview,
                color);
        }


        // =========================================================
        // Axis Color
        // =========================================================

        private void AxisColorButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string? color =
                SelectColor(
                    Settings.AxisColor);

            if (color == null)
                return;

            Settings.AxisColor =
                color;

            SetPreviewColor(
                AxisColorPreview,
                color);
        }


        // =========================================================
        // Grid Color
        // =========================================================

        private void GridColorButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string? color =
                SelectColor(
                    Settings.GridColor);

            if (color == null)
                return;

            Settings.GridColor =
                color;

            SetPreviewColor(
                GridColorPreview,
                color);
        }


        // =========================================================
        // Save
        // =========================================================

        private void SaveButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (LineWidthComboBox.SelectedItem
                is ComboBoxItem item &&
                double.TryParse(
                    item.Tag?.ToString(),
                    out double width))
            {
                Settings.LineWidth =
                    width;
            }


            DialogResult = true;
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
    }
}