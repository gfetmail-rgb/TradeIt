using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfButton = System.Windows.Controls.Button;

using TradeIt.Models;

namespace TradeIt
{
    public partial class MainWindow
    {
        static MainWindow()
        {
            EventManager.RegisterClassHandler(
                typeof(WpfButton),
                WpfButton.ClickEvent,
                new RoutedEventHandler(PrepareSymbolListBeforeButtonAction));
        }

        private static void PrepareSymbolListBeforeButtonAction(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not WpfButton button ||
                button.Name != "DeleteSymbolsButton")
            {
                return;
            }

            if (Window.GetWindow(button) is not MainWindow window ||
                window._selectedPortfolio == null ||
                window._allSymbols == null ||
                window._allSymbols.Count == 0)
            {
                return;
            }

            window._selectedPortfolio.Symbols =
                window._allSymbols
                    .Select(CloneSymbol)
                    .ToList();

            window._selectedPortfolio.UseExplicitSymbolList = true;
        }

        private static SymbolInfo CloneSymbol(SymbolInfo source)
        {
            return new SymbolInfo
            {
                Symbol = source.Symbol,
                DisplayName = source.DisplayName,
                FilePath = source.FilePath,
                RowNumber = source.RowNumber,
                IsSelected = source.IsSelected,
                LastTradeDate = source.LastTradeDate,
                LastVolume = source.LastVolume,
                LastOpen = source.LastOpen,
                LastHigh = source.LastHigh,
                LastLow = source.LastLow,
                LastClose = source.LastClose,
                LastFinalFee = source.LastFinalFee
            };
        }
    }
}
