using System.Linq;

namespace TradeIt
{
    public partial class MainWindow
    {
        /// <summary>
        /// Refreshes the portfolio list after a portfolio is saved by the editor,
        /// without closing the editor window.
        /// </summary>
        public void RefreshPortfoliosAfterEditorSave(string portfolioName)
        {
            try
            {
                string? currentPortfolioName = _selectedPortfolio?.Name;

                _portfolios = _portfolioManager.LoadAll();
                PortfolioComboBox.ItemsSource = null;
                PortfolioComboBox.ItemsSource = _portfolios;

                string targetName = !string.IsNullOrWhiteSpace(portfolioName)
                    ? portfolioName
                    : currentPortfolioName ?? string.Empty;

                Portfolio? target = _portfolios.FirstOrDefault(
                    x => x.Name == targetName);

                if (target != null)
                    PortfolioComboBox.SelectedItem = target;
                else if (_portfolios.Count > 0)
                    PortfolioComboBox.SelectedIndex = 0;
            }
            catch (System.Exception ex)
            {
                WpfMessageBox.Show(
                    ex.ToString(),
                    "خطا در به‌روزرسانی سبدها",
                    WpfMessageBoxButton.OK,
                    WpfMessageBoxImage.Error);
            }
        }
    }
}
