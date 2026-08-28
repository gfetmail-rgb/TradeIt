using System;
using System.Windows;
using System.Windows.Threading;

namespace TradeIt
{
    public partial class MainWindow
    {
        private bool _startupSelectionCleared;

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            if (_startupSelectionCleared)
                return;

            _startupSelectionCleared = true;

            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    _selectedPortfolio = null;
                    _allSymbols.Clear();
                    SymbolsDataGrid.ItemsSource = null;
                    SymbolsDataGrid.SelectedItem = null;
                    PortfolioComboBox.SelectedIndex = -1;
                    StatusTextBlock.Text = _portfolios.Count > 0
                        ? "سبد را انتخاب کنید."
                        : "هنوز سبدی تعریف نشده است.";
                }),
                DispatcherPriority.Loaded);
        }
    }
}
