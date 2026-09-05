using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TradeIt.Models;
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

            // The legacy partial initializes the controls on Loaded as well.
            // Defer one dispatcher turn so the architecture layer can take ownership
            // of those controls without changing their construction or layout.
            window.Dispatcher.BeginInvoke(
                new Action(window.AttachSymbolFilterArchitecture),
                System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void AttachSymbolFilterArchitecture()
        {
            if (_symbolFilterArchitectureAttached || !_symbolFiltersInitialized)
                return;

            DetachLegacySymbolFilterHandlers();

            SymbolSearchTextBox.TextChanged += SymbolFilterArchitecture_TextChanged;
            _nameFilterTextBox!.TextChanged += SymbolFilterArchitecture_TextChanged;
            _nameFilterComboBox!.SelectionChanged += SymbolFilterArchitecture_SelectionChanged;
            _daysWithoutTradeCheckBox!.Checked += SymbolFilterArchitecture_RoutedChanged;
            _daysWithoutTradeCheckBox.Unchecked += SymbolFilterArchitecture_RoutedChanged;
            _daysWithoutTradeTextBox!.TextChanged += SymbolFilterArchitecture_TextChanged;
            _daysWithTradeCheckBox!.Checked += SymbolFilterArchitecture_RoutedChanged;
            _daysWithTradeCheckBox.Unchecked += SymbolFilterArchitecture_RoutedChanged;
            _daysWithTradeTextBox!.TextChanged += SymbolFilterArchitecture_TextChanged;
            _volumeFilterCheckBox!.Checked += SymbolFilterArchitecture_RoutedChanged;
            _volumeFilterCheckBox.Unchecked += SymbolFilterArchitecture_RoutedChanged;
            _volumeAverageDaysTextBox!.TextChanged += SymbolFilterArchitecture_TextChanged;
            _volumeMultiplierTextBox!.TextChanged += SymbolFilterArchitecture_TextChanged;

            foreach (var row in _priceFilterControls)
            {
                row.Enabled.Checked += SymbolFilterArchitecture_RoutedChanged;
                row.Enabled.Unchecked += SymbolFilterArchitecture_RoutedChanged;
                row.LeftField.SelectionChanged += SymbolFilterArchitecture_SelectionChanged;
                row.LeftDays.TextChanged += SymbolFilterArchitecture_TextChanged;
                row.Comparison.SelectionChanged += SymbolFilterArchitecture_SelectionChanged;
                row.RightField.SelectionChanged += SymbolFilterArchitecture_SelectionChanged;
                row.RightDays.TextChanged += SymbolFilterArchitecture_TextChanged;
            }

            _symbolFilterArchitectureAttached = true;
            _ = ApplySymbolFiltersThroughEngineAsync();
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
            _volumeFilterCheckBox.Unchecked -= SymbolFilterInputChanged;
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

        private async Task ApplySymbolFiltersThroughEngineAsync()
        {
            if (!_symbolFilterArchitectureAttached || !_symbolFiltersInitialized || _selectedPortfolio == null)
                return;

            if (!ReferenceEquals(_symbolFilterEnginePortfolio, _selectedPortfolio))
            {
                _symbolFilterEngine.ClearCache();
                _symbolFilterEnginePortfolio = _selectedPortfolio;
            }

            _symbolFilterCts?.Cancel();
            _symbolFilterCts?.Dispose();
            _symbolFilterCts = new CancellationTokenSource();
            CancellationToken token = _symbolFilterCts.Token;

            try
            {
                _symbolFiltersApplying = true;
                CaptureFilterSettings();
                _symbolFiltersApplying = false;

                var results = await _symbolFilterEngine.ApplyAsync(
                    _allSymbols,
                    SymbolSearchTextBox.Text,
                    _symbolFilterSettings,
                    token,
                    symbol => _symbolDataService.LoadBars(symbol, _selectedPortfolio!));

                token.ThrowIfCancellationRequested();
                SymbolsDataGrid.ItemsSource = results;

                if (_symbolFilterStatusTextBlock != null)
                    _symbolFilterStatusTextBlock.Text = $"نتیجه: {results.Count:N0} نماد";
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (_symbolFilterStatusTextBlock != null)
                    _symbolFilterStatusTextBlock.Text = $"خطا در فیلتر: {ex.Message}";
            }
            finally
            {
                _symbolFiltersApplying = false;
            }
        }
    }
}
