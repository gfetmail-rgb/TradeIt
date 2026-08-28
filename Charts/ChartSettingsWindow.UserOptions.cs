using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfButton = System.Windows.Controls.Button;

namespace TradeIt.Charts
{
    public partial class ChartSettingsWindow
    {
        private static readonly bool _saveHandlerRegistered = RegisterSaveHandler();

        private ComboBox? _crosshairPatternComboBox;
        private ComboBox? _crosshairLineWidthComboBox;
        private Border? _crosshairColorPreview;
        private string _crosshairColor = "#909090";

        private static bool RegisterSaveHandler()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartSettingsWindow),
                WpfButton.ClickEvent,
                new RoutedEventHandler(ChartSettingsSaveHandler),
                true);
            return true;
        }

        private static void ChartSettingsSaveHandler(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartSettingsWindow window ||
                e.OriginalSource is not WpfButton button ||
                button.Content?.ToString() != "ذخیره")
            {
                return;
            }

            if (window.OpenChartInNewTabCheckBox != null)
            {
                window.Settings.OpenChartInNewTab =
                    window.OpenChartInNewTabCheckBox.IsChecked == true;
            }

            if (window.GridPatternComboBox?.SelectedItem is ComboBoxItem patternItem)
            {
                window.Settings.GridPattern = patternItem.Tag?.ToString() ?? "Solid";
            }

            if (window.GridLineWidthComboBox?.SelectedItem is ComboBoxItem widthItem &&
                double.TryParse(widthItem.Tag?.ToString(), out double width))
            {
                window.Settings.GridLineWidth = width;
            }

            if (window._crosshairPatternComboBox?.SelectedItem is ComboBoxItem crosshairPatternItem)
            {
                window.Settings.CrosshairPattern = crosshairPatternItem.Tag?.ToString() ?? "Dotted";
            }

            if (window._crosshairLineWidthComboBox?.SelectedItem is ComboBoxItem crosshairWidthItem &&
                double.TryParse(crosshairWidthItem.Tag?.ToString(), out double crosshairWidth))
            {
                window.Settings.CrosshairLineWidth = crosshairWidth;
            }

            window.Settings.CrosshairColor = window._crosshairColor;

            ChartSettingsManager.Save(window.Settings);
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            OpenChartInNewTabCheckBox.IsChecked = Settings.OpenChartInNewTab;

            foreach (ComboBoxItem item in GridPatternComboBox.Items)
            {
                if (string.Equals(item.Tag?.ToString(), Settings.GridPattern, StringComparison.OrdinalIgnoreCase))
                {
                    GridPatternComboBox.SelectedItem = item;
                    break;
                }
            }

            foreach (ComboBoxItem item in GridLineWidthComboBox.Items)
            {
                if (double.TryParse(item.Tag?.ToString(), out double value) &&
                    Math.Abs(value - Settings.GridLineWidth) < 0.001)
                {
                    GridLineWidthComboBox.SelectedItem = item;
                    break;
                }
            }

            BuildCrosshairControls();
        }

        private void BuildCrosshairControls()
        {
            if (_crosshairPatternComboBox != null)
                return;

            _crosshairColor = Settings.CrosshairColor;

            var scrollViewer = Content as Grid;
            if (scrollViewer == null)
                return;

            var scroll = scrollViewer.Children.Count > 0
                ? scrollViewer.Children[0] as ScrollViewer
                : null;
            var stack = scroll?.Content as StackPanel;
            if (stack == null)
                return;

            var group = new GroupBox
            {
                Header = "Crosshair",
                Margin = new Thickness(0, 0, 0, 10)
            };

            var grid = new Grid { Margin = new Thickness(10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            grid.Children.Add(new TextBlock { Text = "رنگ Crosshair:", VerticalAlignment = VerticalAlignment.Center });

            _crosshairColorPreview = new Border
            {
                Width = 32,
                Height = 25,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(_crosshairColorPreview, 1);
            grid.Children.Add(_crosshairColorPreview);

            var colorButton = new WpfButton
            {
                Content = "انتخاب رنگ",
                Width = 100,
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            colorButton.Click += CrosshairColorButton_Click;
            Grid.SetColumn(colorButton, 2);
            grid.Children.Add(colorButton);

            var patternLabel = new TextBlock { Text = "استایل خط:", VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(patternLabel, 1);
            grid.Children.Add(patternLabel);

            _crosshairPatternComboBox = new ComboBox
            {
                Width = 120,
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            _crosshairPatternComboBox.Items.Add(new ComboBoxItem { Content = "یکنواخت", Tag = "Solid" });
            _crosshairPatternComboBox.Items.Add(new ComboBoxItem { Content = "نقطه‌چین", Tag = "Dotted" });
            _crosshairPatternComboBox.Items.Add(new ComboBoxItem { Content = "خط‌چین", Tag = "Dashed" });
            _crosshairPatternComboBox.Items.Add(new ComboBoxItem { Content = "خط‌چین متراکم", Tag = "DenselyDashed" });
            Grid.SetRow(_crosshairPatternComboBox, 1);
            Grid.SetColumn(_crosshairPatternComboBox, 2);
            grid.Children.Add(_crosshairPatternComboBox);

            var widthLabel = new TextBlock { Text = "ضخامت خط:", VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(widthLabel, 2);
            grid.Children.Add(widthLabel);

            _crosshairLineWidthComboBox = new ComboBox
            {
                Width = 120,
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            foreach (double width in new[] { 0.5, 1.0, 1.5, 2.0, 3.0 })
            {
                _crosshairLineWidthComboBox.Items.Add(new ComboBoxItem
                {
                    Content = width.ToString("0.##"),
                    Tag = width.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
            }
            Grid.SetRow(_crosshairLineWidthComboBox, 2);
            Grid.SetColumn(_crosshairLineWidthComboBox, 2);
            grid.Children.Add(_crosshairLineWidthComboBox);

            group.Content = grid;
            stack.Children.Insert(Math.Max(0, stack.Children.Count - 1), group);

            SetPreviewColor(_crosshairColorPreview, _crosshairColor);

            foreach (ComboBoxItem item in _crosshairPatternComboBox.Items)
            {
                if (string.Equals(item.Tag?.ToString(), Settings.CrosshairPattern, StringComparison.OrdinalIgnoreCase))
                {
                    _crosshairPatternComboBox.SelectedItem = item;
                    break;
                }
            }

            foreach (ComboBoxItem item in _crosshairLineWidthComboBox.Items)
            {
                if (double.TryParse(item.Tag?.ToString(), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double value) &&
                    Math.Abs(value - Settings.CrosshairLineWidth) < 0.001)
                {
                    _crosshairLineWidthComboBox.SelectedItem = item;
                    break;
                }
            }

            if (_crosshairPatternComboBox.SelectedItem == null)
                _crosshairPatternComboBox.SelectedIndex = 1;
            if (_crosshairLineWidthComboBox.SelectedItem == null)
                _crosshairLineWidthComboBox.SelectedIndex = 1;
        }

        private void CrosshairColorButton_Click(object? sender, RoutedEventArgs e)
        {
            string? color = SelectColor(_crosshairColor);
            if (color == null)
                return;

            _crosshairColor = color;
            if (_crosshairColorPreview != null)
                SetPreviewColor(_crosshairColorPreview, color);
        }
    }
}