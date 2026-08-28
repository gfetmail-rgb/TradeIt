using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TradeIt.Charts;
using TradeIt.Models;

namespace TradeIt
{
    public partial class MainWindow
    {
        private static readonly bool _chartClickSettingHandlerRegistered = RegisterChartClickSettingHandler();

        private static bool RegisterChartClickSettingHandler()
        {
            EventManager.RegisterClassHandler(
                typeof(TextBlock),
                UIElement.MouseLeftButtonUpEvent,
                new MouseButtonEventHandler(ChartNameClassClickHandler),
                true);
            return true;
        }

        private static async void ChartNameClassClickHandler(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled)
                return;

            if (sender is not TextBlock textBlock ||
                textBlock.Tag?.ToString() != "SymbolName" ||
                textBlock.DataContext is not SymbolInfo symbol)
                return;

            if (Window.GetWindow(textBlock) is not MainWindow window ||
                !IsInsideSymbolGrid(textBlock, window.SymbolsDataGrid) ||
                window._selectedPortfolio == null)
                return;

            e.Handled = true;
            ChartSettings settings = ChartSettingsManager.Current;

            if (settings.OpenChartInNewTab)
                await window.OpenChartTabAsync(symbol, window._selectedPortfolio, false);
            else
                await window.OpenSharedChartTabAsync(symbol, window._selectedPortfolio);
        }

        private static bool IsInsideSymbolGrid(DependencyObject child, DependencyObject grid)
        {
            DependencyObject? current = child;
            while (current != null)
            {
                if (ReferenceEquals(current, grid))
                    return true;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return false;
        }
    }
}