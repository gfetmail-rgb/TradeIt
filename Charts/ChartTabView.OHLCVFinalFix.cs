using System;
using System.Windows;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _ohlcvFinalFixRegistered = RegisterOHLCVFinalFix();
        private bool _ohlcvFinalFixInitialized;

        private static bool RegisterOHLCVFinalFix()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OHLCVFinalFix_Loaded));
            return true;
        }

        private static void OHLCVFinalFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart || chart._ohlcvFinalFixInitialized)
                return;

            chart._ohlcvFinalFixInitialized = true;
            chart.Dispatcher.BeginInvoke(
                new Action(chart.AttachOHLCVFinalFix),
                DispatcherPriority.ApplicationIdle);
        }

        private void AttachOHLCVFinalFix()
        {
            Chart.MouseMove -= OHLCVFinalFix_MouseMove;
            Chart.MouseMove += OHLCVFinalFix_MouseMove;
            Chart.MouseLeave -= OHLCVFinalFix_MouseLeave;
            Chart.MouseLeave += OHLCVFinalFix_MouseLeave;

            if (_bars.Count > 0)
                UpdateOHLCVInfo(_bars.Count - 1);
        }

        private void OHLCVFinalFix_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            try
            {
                if (_bars.Count == 0 ||
                    !TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates coordinates))
                    return;

                int index = FindNearestBarIndex(coordinates.X);
                if (index >= 0)
                    UpdateOHLCVInfo(index);
            }
            catch
            {
            }
        }

        private void OHLCVFinalFix_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_bars.Count > 0)
                UpdateOHLCVInfo(_bars.Count - 1);
        }
    }
}
