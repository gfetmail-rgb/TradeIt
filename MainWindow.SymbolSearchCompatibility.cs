using System.Windows.Controls;

namespace TradeIt
{
    public partial class MainWindow
    {
        // Compatibility member for legacy code in MainWindow.xaml.cs.
        // The visual search box was intentionally removed from MainWindow.xaml.
        private readonly TextBox SymbolSearchTextBox = new TextBox();
    }
}
