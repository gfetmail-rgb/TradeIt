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
            if (e.OriginalSource is not System.Windows.Controls.Button button)
                return;

            MainWindow? window = Window.GetWindow(button) as MainWindow;
            if (window == null)
                return;

            if (ReferenceEquals(button, window.FullScreenButton))
            {
                window.Dispatcher.BeginInvoke(
                    new Action(window.ApplyFullScreenChartLayout),
                    DispatcherPriority.ApplicationIdle);
            }
            else if (ReferenceEquals(button, window.FullScreenExitButton))
            {
                window.Dispatcher.BeginInvoke(
                    new Action(window.ApplyNormalChartLayout),
                    DispatcherPriority.ApplicationIdle);
            }
        }

        private void ApplyFullScreenChartLayout()
        {
            if (!_isFullScreen)
                return;

            try
            {
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Maximized;

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

                if (MainContent.ColumnDefinitions.Count > 1)
                    MainContent.ColumnDefinitions[1].Width = new GridLength(0);

                foreach (UIElement child in MainContent.Children)
                {
                    if (child is System.Windows.Controls.GridSplitter splitter)
                    {
                        splitter.Visibility = Visibility.Collapsed;
                        splitter.Width = 0;
                    }
                }

                Grid.SetColumn(ChartArea, 2);
                ChartPanelColumn.Width = new GridLength(1, GridUnitType.Star);
                ChartArea.Visibility = Visibility.Visible;
                ChartTabs.Visibility = Visibility.Visible;

                FullScreenExitButton.Visibility = Visibility.Visible;
                System.Windows.Controls.Panel.SetZIndex(FullScreenExitButton, 10000);

                UpdateLayout();
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

        private void ApplyNormalChartLayout()
        {
            try
            {
                FullScreenExitButton.Visibility = Visibility.Collapsed;

                Grid.SetRow(MainContent, 1);
                Grid.SetColumn(MainContent, 0);
                Grid.SetRowSpan(MainContent, 1);
                Grid.SetColumnSpan(MainContent, 2);

                SymbolsPanelColumn.Width = new GridLength(300);
                if (MainContent.ColumnDefinitions.Count > 1)
                    MainContent.ColumnDefinitions[1].Width = new GridLength(5);
                ChartPanelColumn.Width = new GridLength(1, GridUnitType.Star);

                foreach (UIElement child in MainContent.Children)
                {
                    if (child is System.Windows.Controls.GridSplitter splitter)
                    {
                        splitter.Visibility = Visibility.Visible;
                        splitter.Width = 5;
                    }
                }

                Grid.SetColumn(ChartArea, 2);
                SymbolsPanel.Visibility = Visibility.Visible;
                ChartArea.Visibility = Visibility.Visible;
                ChartTabs.Visibility = Visibility.Visible;

                WindowStyle = _previousWindowStyle;
                ResizeMode = _previousResizeMode;
                WindowState = _previousWindowState;

                TopToolbar.Visibility = Visibility.Visible;
                StatusBar.Visibility = Visibility.Visible;
                TopToolbarRow.Height = _previousRootRow0Height;
                MainContentRow.Height = _previousRootRow1Height;
                StatusBarRow.Height = _previousRootRow2Height;

                UpdateLayout();
                RootLayout.UpdateLayout();
                MainContent.UpdateLayout();
                ChartArea.UpdateLayout();
                ChartTabs.UpdateLayout();
                InvalidateVisualTree(ChartArea);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Normal chart layout restore failed: {ex}");
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
