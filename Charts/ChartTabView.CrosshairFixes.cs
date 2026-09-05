using System;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Input;
using TradeIt.Models;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private void UpdateCrosshairAxisLabel(int barIndex)
        {
            if (_bars == null || _bars.Count == 0 || barIndex < 0 || barIndex >= _bars.Count || _crosshair == null)
                return;

            double x = _continuousTimeAxisApplied
                ? ContinuousX(barIndex)
                : GetBarDateTime(_bars[barIndex], barIndex).ToOADate();

            _crosshair.Position = new ScottPlot.Coordinates(x, _crosshair.Position.Y);
            _crosshair.VerticalLine.Text = GetCrosshairXLabel(barIndex);
        }

        private string GetCrosshairXLabel(int barIndex)
        {
            if (barIndex < 0 || barIndex >= _bars.Count)
                return string.Empty;

            MarketBar bar = _bars[barIndex];
            string sourceDate = bar.JalaliDate?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(sourceDate))
                return $"کندل {barIndex + 1}";

            string time = bar.Time?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(time))
                return $"{sourceDate} {time}";

            return sourceDate;
        }

        private string GetSourceDateLabel(int barIndex)
        {
            if (barIndex < 0 || barIndex >= _bars.Count)
                return string.Empty;

            return GetCrosshairXLabel(barIndex);
        }

        private void InitializeCrosshairAtInitialPosition()
        {
            if (_crosshair == null || !_chartVisible || _bars.Count == 0)
                return;

            int index = _bars.Count - 1;
            double x = _continuousTimeAxisApplied
                ? ContinuousX(index)
                : GetBarDateTime(_bars[index], index).ToOADate();
            double y = _bars[index].Close;

            _crosshair.Position = new ScottPlot.Coordinates(x, y);
            _crosshair.HorizontalLine.Text = y.ToString("N2");
            _crosshair.VerticalLine.Text = GetCrosshairXLabel(index);
            _crosshairVisible = true;
            _crosshairMouseInside = true;
            _crosshair.IsVisible = true;
        }

        private void ConfigureBottomAxisForCrosshair()
        {
            // DrawChart() and the final chart-fix pass own the bottom-axis tick generator.
        }

        private static readonly bool _crosshairButtonSafeFixRegistered = RegisterCrosshairButtonSafeFix();
        private bool _crosshairButtonSafeFixAttached;

        private static bool RegisterCrosshairButtonSafeFix()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(CrosshairButtonSafeFix_Loaded));
            return true;
        }

        private static void CrosshairButtonSafeFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.AttachCrosshairButtonSafeFix();
        }

        private void AttachCrosshairButtonSafeFix()
        {
            if (_crosshairButtonSafeFixAttached)
                return;

            _crosshairButtonSafeFixAttached = true;
            CrosshairButton.Click += CrosshairButtonSafeFix_Click;
        }

        private void CrosshairButtonSafeFix_Click(object sender, RoutedEventArgs e)
        {
            _crosshairVisible = !_crosshairVisible;

            if (_crosshair != null)
            {
                _crosshair.IsVisible = _crosshairVisible &&
                                       _chartVisible &&
                                       (_crosshairMouseInside || !_hasInitialView);
            }

            CrosshairButton.IsChecked = _crosshairVisible;
            CrosshairButton.Content = _crosshairVisible ? "✚" : "✚";
            SaveCurrentDisplayState();

            Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() =>
                {
                    if (!IsLoaded || Chart == null)
                        return;
                    Chart.Refresh();
                }));

            e.Handled = true;
        }

        protected override void OnInitialized(System.EventArgs e)
        {
            base.OnInitialized(e);
            Chart.PreviewMouseDown += Chart_PreviewMouseDownForCrosshair;
        }

        private void Chart_PreviewMouseDownForCrosshair(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Middle) return;
            if (_crosshair == null || !_crosshairVisible) return;

            _crosshairVisible = false;
            _crosshairMouseInside = false;
            _crosshair.IsVisible = false;
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | Crosshair خاموش";
            Chart.Refresh();
            SaveCurrentDisplayState();
            e.Handled = true;
        }
    }
}