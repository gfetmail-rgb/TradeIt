using System;
using System.Windows;
using System.Windows.Controls;
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
                DispatcherPriority.Render);
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

                Grid.SetRow(MainContent, 1);
                Grid.SetColumn(MainContent, 0);
                Grid.SetRowSpan(MainContent, 1);
                Grid.SetColumnSpan(MainContent, 2);

                SymbolsPanel.Visibility = Visibility.Collapsed;
                SymbolsPanelColumn.Width = new GridLength(0);

                // MainContent column 1 is the splitter. Collapse both the
                // column and splitter so the chart gets the complete width.
                if (MainContent.ColumnDefinitions.Count > 1)
                    MainContent.ColumnDefinitions[1].Width = new GridLength(0);

                foreach (UIElement child in MainContent.Children)
                {
                    if (child is GridSplitter splitter)
                    {
                        splitter.Visibility = Visibility.Collapsed;
                        splitter.Width = 0;
                    }
                }

                // ChartArea belongs to column 2. Keep it there; moving it to
                // column 0 would place it underneath the hidden symbol panel.
                Grid.SetColumn(ChartArea, 2);
                ChartPanelColumn.Width = new GridLength(1, GridUnitType.Star);
                ChartArea.Visibility = Visibility.Visible;
                ChartTabs.Visibility = Visibility.Visible;

                if (ChartTabs.SelectedContent is FrameworkElement selectedChart)
                    selectedChart.Visibility = Visibility.Visible;

                RootLayout.UpdateLayout();
                MainContent.UpdateLayout();
                ChartArea.UpdateLayout();
                ChartTabs.UpdateLayout();
                InvalidateVisualTree(ChartArea);
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