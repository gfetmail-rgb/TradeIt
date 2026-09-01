using System;
using System.Windows;
using System.Windows.Controls;

namespace TradeIt.Portfolios
{
    public partial class PortfolioManagementWindow
    {
        private bool _detailsFixHandlerAttached;

        private void AttachPortfolioDetailsFix()
        {
            if (_detailsFixHandlerAttached)
                return;

            _detailsFixHandlerAttached = true;
            PortfolioListBox.SelectionChanged += PortfolioDetailsFix_SelectionChanged;
        }

        private void PortfolioDetailsFix_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_selectedPortfolio == null)
                return;

            ShowCalendarAndDateTimeDetails(_selectedPortfolio);
        }

        private void ShowCalendarAndDateTimeDetails(Models.Portfolio portfolio)
        {
            var dataSource = portfolio.DataSource;

            CalendarTextBox.Text = dataSource?.Calendar switch
            {
                "Persian" => "شمسی",
                "Gregorian" => "میلادی",
                string value when !string.IsNullOrWhiteSpace(value) => value,
                _ => ""
            };

            DateFormatTextBox.Text = dataSource?.DateFormat ?? "";
            TimeFormatTextBox.Text = dataSource?.TimeFormat ?? "";
        }

        private void PortfolioManagementWindow_DetailsFixLoaded(object sender, RoutedEventArgs e)
        {
            AttachPortfolioDetailsFix();
            if (_selectedPortfolio != null)
                ShowCalendarAndDateTimeDetails(_selectedPortfolio);
        }
    }
}
