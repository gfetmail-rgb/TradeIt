using System.Linq;
using System.Windows;
using System.Windows.Controls;

using TradeIt.Models;

namespace TradeIt
{
    public partial class MainWindow
    {
        static MainWindow()
        {
            // این handler قبل از handler معمولی Click اجرا می‌شود.
            // برای Delete، فهرست واقعی نمادهای نمایش‌داده‌شده را به
            // فهرست صریح سبد منتقل می‌کند تا حذف روی سبدهای قدیمی
            // که UseExplicitSymbolList=false دارند نیز قطعی باشد.
            EventManager.RegisterClassHandler(
                typeof(Button),
                Button.ClickEvent,
                new RoutedEventHandler(PrepareSymbolListBeforeButtonAction));
        }

        private static void PrepareSymbolListBeforeButtonAction(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button ||
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

            // همیشه یک snapshot کامل از نمادهای فعلی سبد می‌سازیم.
            // handler اصلی Delete سپس نمادهای تیک‌خورده را از همین
            // فهرست حذف کرده و Save می‌کند.
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
