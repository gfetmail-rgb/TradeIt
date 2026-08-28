using System;
using System.Windows;
using System.Windows.Controls;
using WpfButton = System.Windows.Controls.Button;

namespace TradeIt.Charts
{
    public partial class ChartSettingsWindow
    {
        private static readonly bool _saveHandlerRegistered = RegisterSaveHandler();

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
        }
    }
}