using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TradeIt.Charts;
using TradeIt.Models;

namespace TradeIt
{
    public partial class MainWindow
    {
        private static readonly bool _sharedChartSettingFixRegistered = RegisterSharedChartSettingFix();

        private static bool RegisterSharedChartSettingFix()
        {
            EventManager.RegisterClassHandler(
                typeof(MainWindow),
                UIElement.PreviewMouseLeftButtonUpEvent,
                new MouseButtonEventHandler(SharedChartSettingFix_MouseLeftButtonUp),
                true);
            return true;
        }

        private static async void SharedChartSettingFix_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled || e.ChangedButton != MouseButton.Left)
                return;

            if (e.OriginalSource is not DependencyObject source)
                return;

            TextBlock? symbolText = FindSymbolNameTextBlock(source);
            if (symbolText?.DataContext is not SymbolInfo symbol)
                return;

            if (Window.GetWindow(symbolText) is not MainWindow window ||
                window._selectedPortfolio == null)
                return;

            if (ChartSettingsManager.Current.OpenChartInNewTab)
                return;

            e.Handled = true;
            await window.OpenSharedChartTabAsync(symbol, window._selectedPortfolio);
        }

        private static TextBlock? FindSymbolNameTextBlock(DependencyObject source)
        {
            DependencyObject? current = source;
            while (current != null)
            {
                if (current is TextBlock text && text.Tag?.ToString() == "SymbolName")
                    return text;

                current = current is System.Windows.Media.Visual visual
                    ? System.Windows.Media.VisualTreeHelper.GetParent(visual)
                    : current is FrameworkContentElement content
                        ? content.Parent
                        : null;
            }

            return null;
        }
    }
}
