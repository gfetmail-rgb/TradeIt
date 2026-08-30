using System.Windows;
using System.Windows.Controls;

namespace TradeIt
{
    public partial class MainWindow
    {
        private void SymbolFilterInputChanged(
            object? sender,
            SelectionChangedEventArgs e)
        {
            if (_symbolFiltersApplying)
                return;

            _ = ApplyAllSymbolFiltersAsync();
        }
    }
}
