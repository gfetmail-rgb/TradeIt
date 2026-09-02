using System;
using System.Windows;
using System.Windows.Threading;
using WpfButton = System.Windows.Controls.Button;
using WpfGrid = System.Windows.Controls.Grid;
using WpfRowDefinition = System.Windows.Controls.RowDefinition;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using TradeIt.Models;

namespace TradeIt
{
    public partial class MainWindow
    {
        private bool _clearFiltersUiAdded;
        private static readonly bool _filterClearHandlerRegistered = RegisterFilterClearHandler();

        private static bool RegisterFilterClearHandler()
        {
            EventManager.RegisterClassHandler(typeof(MainWindow), Window.LoadedEvent, new RoutedEventHandler(FilterClearLoadedClassHandler));
            return true;
        }

        private static void FilterClearLoadedClassHandler(object sender, RoutedEventArgs e)
        {
            if (sender is MainWindow window)
                window.Dispatcher.BeginInvoke(new Action(window.AddClearAllFiltersButton), DispatcherPriority.Loaded);
        }

        private void AddClearAllFiltersButton()
        {
            if (_clearFiltersUiAdded || SymbolFilterHost == null)
                return;

            _clearFiltersUiAdded = true;
            SymbolFilterHost.RowDefinitions.Clear();
            SymbolFilterHost.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });
            SymbolFilterHost.RowDefinitions.Add(new WpfRowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            UIElement? existing = SymbolFilterHost.Children.Count > 0 ? SymbolFilterHost.Children[0] : null;
            if (existing != null)
                WpfGrid.SetRow(existing, 1);

            var clearButton = new WpfButton
            {
                Content = "پاک کردن تمام فیلترها",
                Height = 30,
                Padding = new Thickness(12, 2, 12, 2),
                HorizontalAlignment = WpfHorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 0, 6),
                ToolTip = "همه فیلترها را پاک و غیرفعال می‌کند"
            };
            clearButton.Click += ClearAllFiltersButton_Click;
            WpfGrid.SetRow(clearButton, 0);
            SymbolFilterHost.Children.Add(clearButton);
        }

        private void ClearAllFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            _symbolFiltersApplying = true;
            try
            {
                if (_tradeStatusFilterComboBox != null) _tradeStatusFilterComboBox.SelectedIndex = 0;
                if (_nameFilterComboBox != null) _nameFilterComboBox.SelectedIndex = 0;
                if (_nameFilterTextBox != null) _nameFilterTextBox.Clear();
                if (_daysWithoutTradeCheckBox != null) _daysWithoutTradeCheckBox.IsChecked = false;
                if (_daysWithTradeCheckBox != null) _daysWithTradeCheckBox.IsChecked = false;
                if (_volumeFilterCheckBox != null) _volumeFilterCheckBox.IsChecked = false;
                if (_daysWithoutTradeTextBox != null) _daysWithoutTradeTextBox.Text = "5";
                if (_daysWithTradeTextBox != null) _daysWithTradeTextBox.Text = "5";
                if (_volumeAverageDaysTextBox != null) _volumeAverageDaysTextBox.Text = "20";
                if (_volumeMultiplierTextBox != null) _volumeMultiplierTextBox.Text = "2";

                foreach (var row in _priceFilterControls)
                {
                    row.Enabled.IsChecked = false;
                    row.LeftField.SelectedIndex = 0;
                    row.LeftDays.Text = "0";
                    row.Comparison.SelectedIndex = 0;
                    row.RightField.SelectedIndex = 0;
                    row.RightDays.Text = "1";
                }

                _symbolFilterSettings.TradeStatus = TradeStatusFilter.All;
                _symbolFilterSettings.NameFilter = SymbolNameFilter.All;
                _symbolFilterSettings.NameText = "";
                _symbolFilterSettings.DaysWithoutTradeEnabled = false;
                _symbolFilterSettings.DaysWithTradeEnabled = false;
                _symbolFilterSettings.VolumeFilterEnabled = false;
                foreach (var filter in _symbolFilterSettings.PriceFilters)
                    filter.Enabled = false;

                if (_symbolFilterStatusTextBlock != null)
                    _symbolFilterStatusTextBlock.Text = "همه فیلترها پاک و غیرفعال شدند.";
            }
            finally
            {
                _symbolFiltersApplying = false;
            }
            _ = ApplyAllSymbolFiltersAsync();
        }
    }
}
