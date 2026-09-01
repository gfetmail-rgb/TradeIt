using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfComboBoxItem = System.Windows.Controls.ComboBoxItem;
using WpfGroupBox = System.Windows.Controls.GroupBox;
using WpfBorder = System.Windows.Controls.Border;

namespace TradeIt.Charts
{
    public partial class ChartSettingsWindow
    {
        private static readonly bool _saveHandlerRegistered = RegisterSaveHandler();
        private WpfComboBox? _crosshairPatternComboBox;
        private WpfComboBox? _crosshairLineWidthComboBox;
        private WpfBorder? _crosshairColorPreview;
        private WpfComboBox? _volumeWidthComboBox;
        private WpfBorder? _volumeColorPreview;
        private string _crosshairColor = "#909090";
        private string _volumeColor = "#607D8B";

        private static bool RegisterSaveHandler()
        {
            EventManager.RegisterClassHandler(typeof(ChartSettingsWindow), WpfButton.ClickEvent,
                new RoutedEventHandler(ChartSettingsSaveHandler), true);
            return true;
        }

        private static void ChartSettingsSaveHandler(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartSettingsWindow window || e.OriginalSource is not WpfButton button || button.Content?.ToString() != "ذخیره") return;
            if (window.OpenChartInNewTabCheckBox != null) window.Settings.OpenChartInNewTab = window.OpenChartInNewTabCheckBox.IsChecked == true;
            if (window.GridPatternComboBox?.SelectedItem is WpfComboBoxItem pi) window.Settings.GridPattern = pi.Tag?.ToString() ?? "Solid";
            if (window.GridLineWidthComboBox?.SelectedItem is WpfComboBoxItem wi && double.TryParse(wi.Tag?.ToString(), out var w)) window.Settings.GridLineWidth = w;
            if (window.LineWidthComboBox?.SelectedItem is WpfComboBoxItem li && double.TryParse(li.Tag?.ToString(), out var lw)) window.Settings.LineWidth = lw;
            if (window.CandleLineWidthComboBox?.SelectedItem is WpfComboBoxItem ci && double.TryParse(ci.Tag?.ToString(), out var clw)) window.Settings.CandleLineWidth = clw;
            if (window.BarLineWidthComboBox?.SelectedItem is WpfComboBoxItem bi && double.TryParse(bi.Tag?.ToString(), out var blw)) window.Settings.BarLineWidth = blw;
            if (window._volumeWidthComboBox?.SelectedItem is WpfComboBoxItem vwi && double.TryParse(vwi.Tag?.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var vw)) window.Settings.VolumeBarWidth = vw;
            window.Settings.VolumeColor = window._volumeColor;
            if (window._crosshairPatternComboBox?.SelectedItem is WpfComboBoxItem cpi) window.Settings.CrosshairPattern = cpi.Tag?.ToString() ?? "Dotted";
            if (window._crosshairLineWidthComboBox?.SelectedItem is WpfComboBoxItem cwi && double.TryParse(cwi.Tag?.ToString(), out var cw)) window.Settings.CrosshairLineWidth = cw;
            window.Settings.CrosshairColor = window._crosshairColor;
            window.Settings.HasUserSavedSettings = true;
            ChartSettingsManager.Save(window.Settings);
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            OpenChartInNewTabCheckBox.IsChecked = Settings.OpenChartInNewTab;
            SelectComboValue(LineWidthComboBox, Settings.LineWidth);
            SelectComboValue(CandleLineWidthComboBox, Settings.CandleLineWidth);
            SelectComboValue(BarLineWidthComboBox, Settings.BarLineWidth);
            foreach (WpfComboBoxItem item in GridPatternComboBox.Items)
                if (string.Equals(item.Tag?.ToString(), Settings.GridPattern, StringComparison.OrdinalIgnoreCase)) { GridPatternComboBox.SelectedItem = item; break; }
            foreach (WpfComboBoxItem item in GridLineWidthComboBox.Items)
                if (double.TryParse(item.Tag?.ToString(), out var value) && Math.Abs(value - Settings.GridLineWidth) < .001) { GridLineWidthComboBox.SelectedItem = item; break; }
            BuildCrosshairControls();
            BuildVolumeControls();
        }

        private static void SelectComboValue(WpfComboBox comboBox, double value)
        {
            foreach (WpfComboBoxItem item in comboBox.Items)
                if (double.TryParse(item.Tag?.ToString(), out var itemValue) && Math.Abs(itemValue - value) < .001) { comboBox.SelectedItem = item; return; }
            if (comboBox.Items.Count > 0) comboBox.SelectedIndex = 0;
        }

        private void BuildVolumeControls()
        {
            if (_volumeWidthComboBox != null) return;
            _volumeColor = Settings.VolumeColor;
            if (Content is not Grid root) return;
            var scroll = root.Children.Count > 0 ? root.Children[0] as ScrollViewer : null;
            var stack = scroll?.Content as StackPanel;
            if (stack == null) return;

            var group = new WpfGroupBox { Header = "حجم", Margin = new Thickness(0, 0, 0, 10) };
            var grid = new Grid { Margin = new Thickness(10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            grid.Children.Add(new TextBlock { Text = "رنگ میله‌های حجم:", VerticalAlignment = VerticalAlignment.Center });
            _volumeColorPreview = new WpfBorder { Width = 32, Height = 25, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1), HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
            Grid.SetColumn(_volumeColorPreview, 1); grid.Children.Add(_volumeColorPreview);
            var colorButton = new WpfButton { Content = "انتخاب رنگ", Width = 100, Height = 28, HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
            colorButton.Click += VolumeColorButton_Click; Grid.SetColumn(colorButton, 2); grid.Children.Add(colorButton);

            var widthLabel = new TextBlock { Text = "ضخامت / پهنای میله:", VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(widthLabel, 1); grid.Children.Add(widthLabel);
            _volumeWidthComboBox = new WpfComboBox { Width = 120, Height = 28, HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
            foreach (double width in new[] { .3, .5, .6, .7, .8, .9, 1.0 })
                _volumeWidthComboBox.Items.Add(new WpfComboBoxItem { Content = width.ToString("0.0"), Tag = width.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            Grid.SetRow(_volumeWidthComboBox, 1); Grid.SetColumn(_volumeWidthComboBox, 2); grid.Children.Add(_volumeWidthComboBox);

            group.Content = grid;
            stack.Children.Insert(Math.Max(0, stack.Children.Count - 1), group);
            SetPreviewColor(_volumeColorPreview, _volumeColor);
            SelectComboValue(_volumeWidthComboBox, Settings.VolumeBarWidth);
        }

        private void VolumeColorButton_Click(object? sender, RoutedEventArgs e)
        {
            var color = SelectColor(_volumeColor);
            if (color == null) return;
            _volumeColor = color;
            if (_volumeColorPreview != null) SetPreviewColor(_volumeColorPreview, color);
        }

        private void BuildCrosshairControls()
        {
            if (_crosshairPatternComboBox != null) return;
            _crosshairColor = Settings.CrosshairColor;
            if (Content is not Grid root) return;
            var scroll = root.Children.Count > 0 ? root.Children[0] as ScrollViewer : null;
            var stack = scroll?.Content as StackPanel;
            if (stack == null) return;

            var group = new WpfGroupBox { Header = "Crosshair", Margin = new Thickness(0, 0, 0, 10) };
            var grid = new Grid { Margin = new Thickness(10) };
            for (int i = 0; i < 3; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            grid.Children.Add(new TextBlock { Text = "رنگ Crosshair:", VerticalAlignment = VerticalAlignment.Center });
            _crosshairColorPreview = new WpfBorder { Width = 32, Height = 25, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1), HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
            Grid.SetColumn(_crosshairColorPreview, 1); grid.Children.Add(_crosshairColorPreview);
            var colorButton = new WpfButton { Content = "انتخاب رنگ", Width = 100, Height = 28, HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
            colorButton.Click += CrosshairColorButton_Click; Grid.SetColumn(colorButton, 2); grid.Children.Add(colorButton);
            var patternLabel = new TextBlock { Text = "استایل خط:", VerticalAlignment = VerticalAlignment.Center }; Grid.SetRow(patternLabel, 1); grid.Children.Add(patternLabel);
            _crosshairPatternComboBox = new WpfComboBox { Width = 120, Height = 28, HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
            _crosshairPatternComboBox.Items.Add(new WpfComboBoxItem { Content = "یکنواخت", Tag = "Solid" });
            _crosshairPatternComboBox.Items.Add(new WpfComboBoxItem { Content = "نقطه‌چین", Tag = "Dotted" });
            _crosshairPatternComboBox.Items.Add(new WpfComboBoxItem { Content = "خط‌چین", Tag = "Dashed" });
            _crosshairPatternComboBox.Items.Add(new WpfComboBoxItem { Content = "خط‌چین متراکم", Tag = "DenselyDashed" });
            Grid.SetRow(_crosshairPatternComboBox, 1); Grid.SetColumn(_crosshairPatternComboBox, 2); grid.Children.Add(_crosshairPatternComboBox);
            var widthLabel = new TextBlock { Text = "ضخامت خط:", VerticalAlignment = VerticalAlignment.Center }; Grid.SetRow(widthLabel, 2); grid.Children.Add(widthLabel);
            _crosshairLineWidthComboBox = new WpfComboBox { Width = 120, Height = 28, HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
            foreach (double width in new[] { .5, 1.0, 1.5, 2.0, 3.0 }) _crosshairLineWidthComboBox.Items.Add(new WpfComboBoxItem { Content = width.ToString("0.##"), Tag = width.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            Grid.SetRow(_crosshairLineWidthComboBox, 2); Grid.SetColumn(_crosshairLineWidthComboBox, 2); grid.Children.Add(_crosshairLineWidthComboBox);
            group.Content = grid; stack.Children.Insert(Math.Max(0, stack.Children.Count - 1), group);
            SetPreviewColor(_crosshairColorPreview, _crosshairColor);
            foreach (WpfComboBoxItem item in _crosshairPatternComboBox.Items) if (string.Equals(item.Tag?.ToString(), Settings.CrosshairPattern, StringComparison.OrdinalIgnoreCase)) { _crosshairPatternComboBox.SelectedItem = item; break; }
            foreach (WpfComboBoxItem item in _crosshairLineWidthComboBox.Items) if (double.TryParse(item.Tag?.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value) && Math.Abs(value - Settings.CrosshairLineWidth) < .001) { _crosshairLineWidthComboBox.SelectedItem = item; break; }
            if (_crosshairPatternComboBox.SelectedItem == null) _crosshairPatternComboBox.SelectedIndex = 1;
            if (_crosshairLineWidthComboBox.SelectedItem == null) _crosshairLineWidthComboBox.SelectedIndex = 1;
        }

        private void CrosshairColorButton_Click(object? sender, RoutedEventArgs e)
        {
            var color = SelectColor(_crosshairColor); if (color == null) return;
            _crosshairColor = color; if (_crosshairColorPreview != null) SetPreviewColor(_crosshairColorPreview, color);
        }
    }
}