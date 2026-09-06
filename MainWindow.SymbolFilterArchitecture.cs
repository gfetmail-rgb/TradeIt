using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TradeIt.Models;
using TradeIt.Portfolios;
using TradeIt.Services;

namespace TradeIt
{
    public partial class MainWindow
    {
        private readonly SymbolFilterEngine _symbolFilterEngine = new();
        private Portfolio? _symbolFilterEnginePortfolio;
        private bool _symbolFilterArchitectureAttached;

        private static readonly bool _symbolFilterArchitectureRegistered = RegisterSymbolFilterArchitecture();

        private static bool RegisterSymbolFilterArchitecture()
        {
            EventManager.RegisterClassHandler(
                typeof(MainWindow),
                Window.LoadedEvent,
                new RoutedEventHandler(SymbolFilterArchitecture_Loaded));
            return true;
        }

        private static void SymbolFilterArchitecture_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not MainWindow window)
                return;

            window.Dispatcher.BeginInvoke(
                new Action(window.AttachSymbolFilterArchitecture),
                System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void DetachLegacySymbolFilterHandlers()
        {
            SymbolSearchTextBox.TextChanged -= SymbolFilterInputChanged;
            _nameFilterTextBox!.TextChanged -= SymbolFilterInputChanged;
            _nameFilterComboBox!.SelectionChanged -= SymbolFilterInputChanged;
            _daysWithoutTradeCheckBox!.Checked -= SymbolFilterInputChanged;
            _daysWithoutTradeCheckBox.Unchecked -= SymbolFilterInputChanged;
            _daysWithoutTradeTextBox!.TextChanged -= SymbolFilterInputChanged;
            _daysWithTradeCheckBox!.Checked -= SymbolFilterInputChanged;
            _daysWithTradeCheckBox.Unchecked -= SymbolFilterInputChanged;
            _daysWithTradeTextBox!.TextChanged -= SymbolFilterInputChanged;
            _volumeFilterCheckBox!.Checked -= SymbolFilterInputChanged;
            _volumeFilterCheckBox!.Unchecked -= SymbolFilterInputChanged;
            _volumeAverageDaysTextBox!.TextChanged -= SymbolFilterInputChanged;
            _volumeMultiplierTextBox!.TextChanged -= SymbolFilterInputChanged;

            foreach (var row in _priceFilterControls)
            {
                row.Enabled.Checked -= SymbolFilterInputChanged;
                row.Enabled.Unchecked -= SymbolFilterInputChanged;
                row.LeftField.SelectionChanged -= SymbolFilterInputChanged;
                row.LeftDays.TextChanged -= SymbolFilterInputChanged;
                row.Comparison.SelectionChanged -= SymbolFilterInputChanged;
                row.RightField.SelectionChanged -= SymbolFilterInputChanged;
                row.RightDays.TextChanged -= SymbolFilterInputChanged;
            }
        }

        private void SymbolFilterArchitecture_RoutedChanged(object? sender, RoutedEventArgs e)
        {
            if (!_symbolFiltersApplying)
                _ = ApplySymbolFiltersThroughEngineAsync();
        }

        private void SymbolFilterArchitecture_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_symbolFiltersApplying)
                _ = ApplySymbolFiltersThroughEngineAsync();
        }

        private void SymbolFilterArchitecture_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_symbolFiltersApplying)
                _ = ApplySymbolFiltersThroughEngineAsync();
        }
    }
}
