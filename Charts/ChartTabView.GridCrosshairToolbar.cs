using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _gridCrosshairToolbarRegistered = RegisterGridCrosshairToolbar();

        private static bool RegisterGridCrosshairToolbar()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(GridCrosshairToolbar_Loaded));
            return true;
        }

        private static void GridCrosshairToolbar_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart) return;

            chart.CrosshairButton.Content = "✚";
            chart.GridButton.Content = "▦";
            chart.CrosshairButton.IsChecked = chart._crosshairVisible;
            chart.GridButton.IsChecked = chart._gridVisible;

            chart.CrosshairButton.AddHandler(ButtonBase.ClickEvent,
                new RoutedEventHandler(chart.GridCrosshairToolbar_Click), true);
            chart.GridButton.AddHandler(ButtonBase.ClickEvent,
                new RoutedEventHandler(chart.GridCrosshairToolbar_Click), true);

            chart.Chart.PreviewMouseMove += chart.GridCrosshairToolbar_MouseMove;
        }

        private void GridCrosshairToolbar_Click(object sender, RoutedEventArgs e)
        {
            if (ReferenceEquals(sender, CrosshairButton))
            {
                CrosshairButton.Content = "✚";
                CrosshairButton.IsChecked = _crosshairVisible;
            }
            else if (ReferenceEquals(sender, GridButton))
            {
                GridButton.Content = "▦";
                GridButton.IsChecked = _gridVisible;
            }
        }

        private void GridCrosshairToolbar_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            CrosshairButton.IsChecked = _crosshairVisible;
            GridButton.IsChecked = _gridVisible;
        }
    }
}
