using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;

namespace TradeIt
{
    public partial class MainWindow
    {
        private WpfCheckBox? _tradedInDaysEnabledCheckBox;
        private WpfTextBox? _tradedInDaysTextBox;
        private DispatcherTimer? _tradedInDaysApplyTimer;
        private bool _filterLayoutFixInitialized;

        private void InitializeSymbolFiltersLayoutFix()
        {
            if (_filterLayoutFixInitialized || SymbolsPanel.Child is not Grid panelGrid)
                return;

            Grid? filterHost = panelGrid.Children.OfType<Grid>()
                .FirstOrDefault(x => Grid.GetRow(x) == 2 && x.Children.OfType<ScrollViewer>().Any());
            if (filterHost == null)
                return;

            _filterLayoutFixInitialized = true;
            panelGrid.RowDefinitions[2].Height = new GridLength(155);

            foreach (UIElement child in panelGrid.Children)
            {
                if (child == filterHost)
                    continue;
                int row = Grid.GetRow(child);
                if (row >= 3)
                    Grid.SetRow(child, row + 1);
            }

            Grid.SetRow(filterHost, 3);
            panelGrid.RowDefinitions.Insert(4, new RowDefinition { Height = GridLength.Auto });
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

            var controls = new StackPanel { Orientation = WpfOrientation.Horizontal };
            _tradedInDaysEnabledCheckBox = new WpfCheckBox
            {
                Content = "فعال",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            };
            _tradedInDaysTextBox = new WpfTextBox
            {
                Width = 55,
                Height = 27,
                Text = "5",
                HorizontalContentAlignment = WpfHorizontalAlignment.Center
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
            // The existing MainWindow.SymbolFilters.cs owns filtering and its SymbolInfo type.
            // This helper only controls layout and adds the UI. The actual filtering is invoked there.
            if (_tradedInDaysEnabledCheckBox?.IsChecked == true)
                ApplySymbolFilter();
        }

        private void SymbolFiltersLayoutFix_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(InitializeSymbolFiltersLayoutFix));
        }
    }
}