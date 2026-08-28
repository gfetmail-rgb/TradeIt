using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TradeIt.Models;

namespace TradeIt
{
    public partial class MainWindow
    {
        private static readonly bool _symbolKeyboardNavigationRegistered = RegisterSymbolKeyboardNavigation();

        private static bool RegisterSymbolKeyboardNavigation()
        {
            EventManager.RegisterClassHandler(
                typeof(DataGrid),
                UIElement.PreviewKeyDownEvent,
                new KeyEventHandler(SymbolGrid_PreviewKeyDown),
                true);
            return true;
        }

        private static async void SymbolGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter && e.Key != Key.Space)
                return;

            if (sender is not DataGrid grid ||
                Window.GetWindow(grid) is not MainWindow window ||
                window._selectedPortfolio == null ||
                grid.Items.Count == 0)
                return;

            if (e.OriginalSource is CheckBox)
                return;

            int currentIndex = grid.SelectedIndex;
            int nextIndex = currentIndex < 0 ? 0 : currentIndex + 1;
            if (nextIndex >= grid.Items.Count)
                nextIndex = 0;

            if (grid.Items[nextIndex] is not SymbolInfo symbol)
                return;

            grid.SelectedIndex = nextIndex;
            grid.ScrollIntoView(symbol);
            e.Handled = true;

            await window.OpenChartTabAsync(symbol, window._selectedPortfolio);
        }
    }
}
