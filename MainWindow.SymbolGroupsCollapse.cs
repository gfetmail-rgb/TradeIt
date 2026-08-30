using System.Windows;
using System.Windows.Media;

namespace TradeIt
{
    public partial class MainWindow
    {
        private bool _symbolsPanelCollapsed;
        private bool _symbolTableGroupCollapsed;
        private bool _symbolFilterGroupCollapsed = true;
        private bool _symbolOperationsGroupCollapsed = true;

        // Register before InitializeComponent; the actual controls are touched only on Loaded.
        private readonly bool _symbolGroupsCollapseLoadedRegistration = RegisterSymbolGroupsCollapseLoaded();

        private bool RegisterSymbolGroupsCollapseLoaded()
        {
            Loaded += MainWindow_SymbolGroupsCollapseLoaded;
            return true;
        }

        private void MainWindow_SymbolGroupsCollapseLoaded(object sender, RoutedEventArgs e)
        {
            InitializeSymbolGroupsCollapseState();

            SymbolFilterGroup.SizeChanged -= SymbolFilterGroup_SizeChanged;
            SymbolFilterGroup.SizeChanged += SymbolFilterGroup_SizeChanged;

            SymbolOperationsGroup.SizeChanged -= SymbolOperationsGroup_SizeChanged;
            SymbolOperationsGroup.SizeChanged += SymbolOperationsGroup_SizeChanged;

            UpdateSymbolFilterCollapseButtonPosition();
            UpdateSymbolOperationsCollapseButtonPosition();
        }

        private void InitializeSymbolGroupsCollapseState()
        {
            _symbolTableGroupCollapsed = false;
            _symbolFilterGroupCollapsed = true;
            _symbolOperationsGroupCollapsed = true;
            _symbolsPanelCollapsed = false;

            SymbolsDataGrid.Visibility = Visibility.Visible;
            SymbolFilterHost.Visibility = Visibility.Collapsed;
            SymbolOperationsContent.Visibility = Visibility.Collapsed;
            SymbolsGroupsContainer.Visibility = Visibility.Visible;

            SymbolsPanelColumn.Width = new GridLength(300);
            SymbolsPanelCollapseButton.Content = "‹";
            SymbolsPanelCollapseButton.ToolTip = "پنهان کردن پنل نمادها";

            SymbolTableCollapseButton.Content = "▲";
            SymbolFilterCollapseButton.Content = "▲";
            SymbolOperationsCollapseButton.Content = "▲";
        }

        private void SymbolTableCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            _symbolTableGroupCollapsed = !_symbolTableGroupCollapsed;
            SymbolsDataGrid.Visibility = _symbolTableGroupCollapsed ? Visibility.Collapsed : Visibility.Visible;
            SymbolTableCollapseButton.Content = _symbolTableGroupCollapsed ? "▼" : "▲";
        }

        private void SymbolFilterCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            _symbolFilterGroupCollapsed = !_symbolFilterGroupCollapsed;
            SymbolFilterHost.Visibility = _symbolFilterGroupCollapsed ? Visibility.Collapsed : Visibility.Visible;
            SymbolFilterCollapseButton.Content = _symbolFilterGroupCollapsed ? "▲" : "▼";
            UpdateSymbolFilterCollapseButtonPosition();
        }

        private void SymbolOperationsCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            _symbolOperationsGroupCollapsed = !_symbolOperationsGroupCollapsed;
            SymbolOperationsContent.Visibility = _symbolOperationsGroupCollapsed ? Visibility.Collapsed : Visibility.Visible;
            SymbolOperationsCollapseButton.Content = _symbolOperationsGroupCollapsed ? "▲" : "▼";
            UpdateSymbolOperationsCollapseButtonPosition();
        }

        private void SymbolFilterGroup_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateSymbolFilterCollapseButtonPosition();
        }

        private void SymbolOperationsGroup_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateSymbolOperationsCollapseButtonPosition();
        }

        private void UpdateSymbolFilterCollapseButtonPosition()
        {
            if (SymbolFilterGroup == null || SymbolFilterCollapseButton == null)
                return;

            double height = SymbolFilterGroup.ActualHeight;
            double buttonHeight = SymbolFilterCollapseButton.ActualHeight > 0 ? SymbolFilterCollapseButton.ActualHeight : 25;
            SymbolFilterCollapseButton.RenderTransform = new TranslateTransform(0, Math.Max(0, height - buttonHeight - 5));
        }

        private void UpdateSymbolOperationsCollapseButtonPosition()
        {
            if (SymbolOperationsGroup == null || SymbolOperationsCollapseButton == null)
                return;

            double height = SymbolOperationsGroup.ActualHeight;
            double buttonHeight = SymbolOperationsCollapseButton.ActualHeight > 0 ? SymbolOperationsCollapseButton.ActualHeight : 25;
            SymbolOperationsCollapseButton.RenderTransform = new TranslateTransform(0, Math.Max(0, height - buttonHeight - 5));
        }

        private void SymbolsPanelCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            _symbolsPanelCollapsed = !_symbolsPanelCollapsed;

            SymbolsGroupsContainer.Visibility = _symbolsPanelCollapsed ? Visibility.Collapsed : Visibility.Visible;
            SymbolsPanelColumn.Width = new GridLength(_symbolsPanelCollapsed ? 32 : 300);
            SymbolsPanelCollapseButton.Content = _symbolsPanelCollapsed ? "›" : "‹";
            SymbolsPanelCollapseButton.ToolTip = _symbolsPanelCollapsed ? "نمایش پنل نمادها" : "پنهان کردن پنل نمادها";
        }
    }
}