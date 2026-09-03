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
                // This handler is the single owner of fullscreen entry.
                // Stop the original instance handler from applying a second,
                // conflicting layout pass.
                e.Handled = true;
                window.EnterChartFullScreen();
            }
            else if (ReferenceEquals(button, window.FullScreenExitButton))
            {
                e.Handled = true;
                window.ExitChartFullScreen();
            }
        }

        private void EnterChartFullScreen()
        {
            if (_isFullScreen)
                return;

            try
            {
                _previousWindowState = WindowState;
                _previousWindowStyle = WindowStyle;
                _previousResizeMode = ResizeMode;

                _previousRootRow0Height = TopToolbarRow.Height;
                _previousRootRow1Height = MainContentRow.Height;
                _previousRootRow2Height = StatusBarRow.Height;

                _previousSymbolsColumnWidth = SymbolsPanelColumn.Width;
                _previousChartColumnWidth = ChartPanelColumn.Width;

                _isFullScreen = true;

                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Maximized;

                TopToolbar.Visibility = Visibility.Collapsed;
                StatusBar.Visibility = Visibility.Collapsed;
                TopToolbarRow.Height = new GridLength(0);
                MainContentRow.Height = new GridLength(1, GridUnitType.Star);
                StatusBarRow.Height = new GridLength(0);

                // MainContent already occupies the complete root width.
                Grid.SetRow(MainContent, 1);
                Grid.SetColumn(MainContent, 0);
                Grid.SetRowSpan(MainContent, 1);
                Grid.SetColumnSpan(MainContent, 2);

                // Collapse the symbol panel and splitter completely.
                SymbolsPanel.Visibility = Visibility.Collapsed;
                SymbolsPanelColumn.MinWidth = 0;
                SymbolsPanelColumn.Width = new GridLength(0);

                if (MainContent.ColumnDefinitions.Count > 1)
                    MainContent.ColumnDefinitions[1].Width = new GridLength(0);

                foreach (UIElement child in MainContent.Children)
                {
                    if (child is System.Windows.Controls.GridSplitter splitter)
                    {
                        splitter.Visibility = Visibility.Collapsed;
                        splitter.IsHitTestVisible = false;
                        splitter.Width = 0;
                    }
                }

                // ChartArea remains in column 2. Since columns 0 and 1 are
                // zero-width, the star column becomes the entire client area.
                Grid.SetColumn(ChartArea, 2);
                ChartPanelColumn.MinWidth = 0;
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
                _isFullScreen = false;
                System.Diagnostics.Debug.WriteLine($"Fullscreen chart layout failed: {ex}");
            }
        }

        private void ExitChartFullScreen()
        {
            if (!_isFullScreen)
                return;

            try
            {
                FullScreenExitButton.Visibility = Visibility.Collapsed;

                SymbolsPanelColumn.MinWidth = 220;
                SymbolsPanelColumn.Width = _previousSymbolsColumnWidth.Value > 0
                    ? _previousSymbolsColumnWidth
                    : new GridLength(300);

                if (MainContent.ColumnDefinitions.Count > 1)
                    MainContent.ColumnDefinitions[1].Width = new GridLength(5);

                ChartPanelColumn.MinWidth = 450;
                ChartPanelColumn.Width = _previousChartColumnWidth.Value.Value > 0
                    ? _previousChartColumnWidth
                    : new GridLength(1, GridUnitType.Star);

                foreach (UIElement child in MainContent.Children)
                {
                    if (child is System.Windows.Controls.GridSplitter splitter)
                    {
                        splitter.Visibility = Visibility.Visible;
                        splitter.IsHitTestVisible = true;
                        splitter.Width = 5;
                    }
                }

                Grid.SetColumn(ChartArea, 2);
                SymbolsPanel.Visibility = Visibility.Visible;
                ChartArea.Visibility = Visibility.Visible;
                ChartTabs.Visibility = Visibility.Visible;

                TopToolbar.Visibility = Visibility.Visible;
                StatusBar.Visibility = Visibility.Visible;
                TopToolbarRow.Height = _previousRootRow0Height;
                MainContentRow.Height = _previousRootRow1Height;
                StatusBarRow.Height = _previousRootRow2Height;

                WindowStyle = _previousWindowStyle;
                ResizeMode = _previousResizeMode;
                WindowState = _previousWindowState;

                _isFullScreen = false;

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