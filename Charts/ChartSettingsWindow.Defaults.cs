using System;
using System.Globalization;
using System.Windows;
using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfComboBoxItem = System.Windows.Controls.ComboBoxItem;
using WpfGrid = System.Windows.Controls.Grid;
using WpfStackPanel = System.Windows.Controls.StackPanel;

namespace TradeIt.Charts
{
    public partial class ChartSettingsWindow
    {
        private static readonly bool _defaultsHandlerRegistered = RegisterDefaultsHandler();
        private WpfButton? _defaultSettingsButton;

        private static bool RegisterDefaultsHandler()
        {
            EventManager.RegisterClassHandler(typeof(ChartSettingsWindow), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(ChartSettingsWindow_LoadedForDefaults));
            return true;
        }

        private static void ChartSettingsWindow_LoadedForDefaults(object sender, RoutedEventArgs e)
        {
            if (sender is ChartSettingsWindow window)
                window.AddDefaultSettingsButton();
        }

        private void AddDefaultSettingsButton()
        {
            if (_defaultSettingsButton != null) return;
            if (Content is not WpfGrid root || root.Children.Count < 2) return;
            if (root.Children[1] is not WpfStackPanel buttons) return;

            _defaultSettingsButton = new WpfButton
            {
                Content = "پیش‌فرض",
                Width = 90,
                Height = 32,
                Margin = new Thickness(0, 0, 8, 0)
            };
            _defaultSettingsButton.Click += DefaultSettingsButton_Click;
            buttons.Children.Insert(0, _defaultSettingsButton);
        }

        private void DefaultSettingsButton_Click(object? sender, RoutedEventArgs e)
        {
            Settings = new ChartSettings();
            Settings.HasUserSavedSettings = true;

            SetPreviewColor(RisingColorPreview, Settings.RisingColor);
            SetPreviewColor(FallingColorPreview, Settings.FallingColor);
            SetPreviewColor(LineColorPreview, Settings.LineColor);
            SetPreviewColor(FigureBackgroundPreview, Settings.FigureBackground);
            SetPreviewColor(DataBackgroundPreview, Settings.DataBackground);
            SetPreviewColor(GridColorPreview, Settings.GridColor);
            SetPreviewColor(AxisColorPreview, Settings.AxisColor);

            SelectDefaultCombo(LineWidthComboBox, Settings.LineWidth);
            SelectDefaultCombo(CandleLineWidthComboBox, Settings.CandleLineWidth);
            SelectDefaultCombo(BarLineWidthComboBox, Settings.BarLineWidth);
            SelectDefaultCombo(GridLineWidthComboBox, Settings.GridLineWidth);
            SelectDefaultTag(GridPatternComboBox, Settings.GridPattern);
            OpenChartInNewTabCheckBox.IsChecked = Settings.OpenChartInNewTab;

            _crosshairColor = Settings.CrosshairColor;
            if (_crosshairColorPreview != null)
                SetPreviewColor(_crosshairColorPreview, _crosshairColor);
            if (_crosshairPatternComboBox != null)
                SelectDefaultTag(_crosshairPatternComboBox, Settings.CrosshairPattern);
            if (_crosshairLineWidthComboBox != null)
                SelectDefaultCombo(_crosshairLineWidthComboBox, Settings.CrosshairLineWidth);
        }

        private static void SelectDefaultCombo(WpfComboBox combo, double value)
        {
            foreach (WpfComboBoxItem item in combo.Items)
            {
                if (double.TryParse(item.Tag?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) &&
                    Math.Abs(parsed - value) < 0.001)
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        }

        private static void SelectDefaultTag(WpfComboBox combo, string value)
        {
            foreach (WpfComboBoxItem item in combo.Items)
            {
                if (string.Equals(item.Tag?.ToString(), value, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
        }
    }
}