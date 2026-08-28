using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace TradeIt
{
    public partial class MainWindow
    {
        private DispatcherTimer? _startupSelectionGuardTimer;
        private bool _startupSelectionGuardActive;

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            StartStartupSelectionGuard();
        }

        private void StartStartupSelectionGuard()
        {
            _startupSelectionGuardActive = true;

            PortfolioComboBox.PreviewMouseLeftButtonDown +=
                StartupPortfolioComboBox_PreviewMouseLeftButtonDown;

            ClearStartupSelectionNow();

            _startupSelectionGuardTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };

            _startupSelectionGuardTimer.Tick += StartupSelectionGuardTimer_Tick;
            _startupSelectionGuardTimer.Start();
        }

        private void StartupPortfolioComboBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StopStartupSelectionGuard();
        }

        private void StartupSelectionGuardTimer_Tick(object? sender, EventArgs e)
        {
            if (_startupSelectionGuardActive)
                ClearStartupSelectionNow();
        }

        private void ClearStartupSelectionNow()
        {
            StopAutoScroll();

            _selectedPortfolio = null;
            _allSymbols.Clear();
            SymbolsDataGrid.ItemsSource = null;
            SymbolsDataGrid.SelectedItem = null;

            PortfolioComboBox.SelectedItem = null;
            PortfolioComboBox.SelectedIndex = -1;

            StatusTextBlock.Text = _portfolios.Count > 0
                ? "یک سبد را انتخاب کنید."
                : "هنوز سبدی تعریف نشده است.";
        }

        private void StopStartupSelectionGuard()
        {
            if (!_startupSelectionGuardActive)
                return;

            _startupSelectionGuardActive = false;

            if (_startupSelectionGuardTimer != null)
            {
                _startupSelectionGuardTimer.Stop();
                _startupSelectionGuardTimer.Tick -= StartupSelectionGuardTimer_Tick;
                _startupSelectionGuardTimer = null;
            }

            PortfolioComboBox.PreviewMouseLeftButtonDown -=
                StartupPortfolioComboBox_PreviewMouseLeftButtonDown;
        }
    }
}