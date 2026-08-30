using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace TradeIt
{
    public partial class MainWindow
    {
        private CheckBox? _tradedInDaysEnabledCheckBox;
        private TextBox? _tradedInDaysTextBox;
        private DispatcherTimer? _tradedInDaysApplyTimer;
        private bool _filterLayoutFixInitialized;

        private void InitializeSymbolFiltersLayoutFix()
        {
            if (_filterLayoutFixInitialized || SymbolsPanel.Child is not Grid panelGrid)
                return;

            // The existing filter initializer inserts the filter host at row 2.
            // Move that host below the stock list and reserve a small, fixed area for the list.
            Grid? filterHost = panelGrid.Children.OfType<Grid>()
                .FirstOrDefault(x => Grid.GetRow(x) == 2 && x.Children.Count == 2);

            if (filterHost == null)
                return;

            _filterLayoutFixInitialized = true;

            if (panelGrid.RowDefinitions.Count >= 6)
            {
                panelGrid.RowDefinitions[2].Height = new GridLength(155);
                Grid.SetRow(filterHost, 3);

                // The original rows 3 and 4 are the auto-scroll controls and action buttons.
                // Keep them below the filters.
                foreach (UIElement child in panelGrid.Children)
                {
                    int row = Grid.GetRow(child);
                    if (child != filterHost && row >= 3)
                        Grid.SetRow(child, row + 1);
                }
                panelGrid.RowDefinitions.Insert(4, new RowDefinition { Height = GridLength.Auto });
            }

            AddTradedInDaysFilter(filterHost);
        }

        private void AddTradedInDaysFilter(Grid filterHost)
        {
            if (filterHost.Children.OfType<ScrollViewer>().FirstOrDefault()?.Content is not StackPanel stack)
                return;

            var row = new StackPanel { Margin = new Thickness(0, 1, 0, 3) };
            row.Children.Add(new TextBlock
            {
                Text = "دارای معامله در X روز گذشته:",
                VerticalAlignment = VerticalAlignment.Center
            });

            var controls = new StackPanel { Orientation = Orientation.Horizontal };
            _tradedInDaysEnabledCheckBox = new CheckBox
            {
                Content = "فعال",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            };
            _tradedInDaysTextBox = new TextBox
            {
                Width = 55,
                Height = 27,
                Text = "5",
                HorizontalContentAlignment = HorizontalAlignment.Center
            };

            _tradedInDaysApplyTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            _tradedInDaysApplyTimer.Tick += (_, _) =>
            {
                _tradedInDaysApplyTimer.Stop();
                ApplyTradedInDaysFilter();
            };

            _tradedInDaysEnabledCheckBox.Checked += (_, _) => ScheduleTradedInDaysFilter();
            _tradedInDaysEnabledCheckBox.Unchecked += (_, _) => ScheduleTradedInDaysFilter();
            _tradedInDaysTextBox.TextChanged += (_, _) => ScheduleTradedInDaysFilter();

            controls.Children.Add(_tradedInDaysEnabledCheckBox);
            controls.Children.Add(_tradedInDaysTextBox);
            row.Children.Add(controls);
            stack.Children.Add(row);
        }

        private void ScheduleTradedInDaysFilter()
        {
            if (_tradedInDaysApplyTimer == null)
                return;
            _tradedInDaysApplyTimer.Stop();
            _tradedInDaysApplyTimer.Start();
        }

        private void ApplyTradedInDaysFilter()
        {
            if (_tradedInDaysEnabledCheckBox?.IsChecked != true ||
                !int.TryParse(_tradedInDaysTextBox?.Text, out int days) || days <= 0 ||
                SymbolsDataGrid.ItemsSource is not IEnumerable<SymbolInfo> current)
                return;

            DateTime latestMarketDate = _allSymbols
                .Where(s => s.LastTradeDate.HasValue)
                .Select(s => s.LastTradeDate!.Value.Date)
                .DefaultIfEmpty()
                .Max();

            if (latestMarketDate == default)
                return;

            DateTime cutoff = latestMarketDate.AddDays(-days);
            List<SymbolInfo> filtered = current
                .Where(s => s.LastTradeDate.HasValue && s.LastTradeDate.Value.Date >= cutoff)
                .ToList();

            for (int i = 0; i < filtered.Count; i++)
                filtered[i].RowNumber = i + 1;

            SymbolsDataGrid.ItemsSource = filtered;
        }

        private void SymbolFiltersLayoutFix_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(InitializeSymbolFiltersLayoutFix));
        }
    }
}