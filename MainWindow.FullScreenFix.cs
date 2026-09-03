using System;
using System.Windows;
using System.Windows.Threading;

namespace TradeIt
{
    public partial class MainWindow
    {
        private static readonly bool _fullScreenFixRegistered = RegisterFullScreenFix();

        private static bool RegisterFullScreenFix()
        {
            EventManager.RegisterClassHandler(
                typeof(MainWindow),
                System.Windows.Controls.Button.ClickEvent,
                new RoutedEventHandler(FullScreenFix_ButtonClick),
                true);
            return true;
        }

        private static void FullScreenFix_ButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not MainWindow window || e.OriginalSource is not System.Windows.Controls.Button button)
                return;

            if (!ReferenceEquals(button, window.FullScreenButton))
                return;

            window.Dispatcher.BeginInvoke(
                new Action(window.ApplyFullScreenChartLayout),
                DispatcherPriority.Loaded);
        }

        private void ApplyFullScreenChartLayout()
        {
            if (!_isFullScreen)
                return;

            try
            {
                TopToolbar.Visibility = Visibility.Collapsed;
                StatusBar.Visibility = Visibility.Collapsed;
                TopToolbarRow.Height = new GridLength(0);
                StatusBarRow.Height = new GridLength(0);
                MainContentRow.Height = new GridLength(1, GridUnitType.Star);

                System.Windows.Controls.Grid.SetRow(MainContent, 1);
                System.Windows.Controls.Grid.SetColumn(MainContent, 0);
                System.Windows.Controls.Grid.SetRowSpan(MainContent, 1);
                System.Windows.Controls.Grid.SetColumnSpan(MainContent, 2);

                SymbolsPanel.Visibility = Visibility.Collapsed;
                SymbolsPanelColumn.Width = new GridLength(0);
                System.Windows.Controls.Grid.SetColumn(ChartArea, 0);
                ChartPanelColumn.Width = new GridLength(1, GridUnitType.Star);

                if (ChartTabs.SelectedContent is FrameworkElement selectedChart)
                {
                    selectedChart.Visibility = Visibility.Visible;
                    selectedChart.UpdateLayout();
                }

                ChartArea.Visibility = Visibility.Visible;
                ChartTabs.Visibility = Visibility.Visible;
                ChartArea.UpdateLayout();
                ChartTabs.UpdateLayout();
                RootLayout.UpdateLayout();
                InvalidateVisualTree(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fullscreen chart layout fix failed: {ex}");
            }
        }

        private static void InvalidateVisualTree(DependencyObject root)
        {
            if (root is UIElement element)
                element.InvalidateVisual();

            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
                InvalidateVisualTree(System.Windows.Media.VisualTreeHelper.GetChild(root, i));
        }
    }
}
