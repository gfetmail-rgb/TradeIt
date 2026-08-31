using System.Windows;
using WpfButton = System.Windows.Controls.Button;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfGrid = System.Windows.Controls.Grid;
using WpfRowDefinition = System.Windows.Controls.RowDefinition;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfRadioButton = System.Windows.Controls.RadioButton;
using WpfUIElement = System.Windows.UIElement;
using WpfVisualTreeHelper = System.Windows.Media.VisualTreeHelper;

namespace TradeIt
{
    public partial class MainWindow
    {
        private bool _symbolsPanelCollapsed;
        private bool _symbolTableGroupCollapsed;
        private bool _symbolFilterGroupCollapsed = true;
        private bool _symbolOperationsGroupCollapsed = true;
        private WpfButton? _clearSymbolFiltersButton;

        private void MainWindow_SymbolGroupsCollapseLoaded(object sender, RoutedEventArgs e)
        {
            InitializeSymbolGroupsCollapseState();
            DockSymbolGroupButtons();
            EnsureClearFiltersButton();
            // EnterFullScreen();
        }

        private void InitializeSymbolGroupsCollapseState()
        {
            _symbolTableGroupCollapsed = false;
            _symbolFilterGroupCollapsed = true;
            _symbolOperationsGroupCollapsed = true;

            SymbolFilterHost.Visibility = Visibility.Collapsed;
            SymbolOperationsContent.Visibility = Visibility.Collapsed;
            SymbolsDataGrid.Visibility = Visibility.Visible;

            SymbolFilterCollapseButton.Content = "▲";
            SymbolOperationsCollapseButton.Content = "▲";
            SymbolTableCollapseButton.Content = "▲";
        }

        private void DockSymbolGroupButtons()
        {
            DockFilterButtonToBottom();
            DockOperationsButtonToBottom();
        }

        private void DockFilterButtonToBottom()
        {
            if (SymbolFilterGroup.Child is not WpfGrid groupGrid)
                return;

            while (groupGrid.RowDefinitions.Count < 3)
                groupGrid.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });

            WpfGrid? headerGrid = null;
            foreach (WpfUIElement child in groupGrid.Children)
            {
                if (child is WpfGrid grid && grid.Children.Contains(SymbolFilterCollapseButton))
                {
                    headerGrid = grid;
                    break;
                }
            }

            if (headerGrid == null)
                return;

            headerGrid.Children.Remove(SymbolFilterCollapseButton);

            var bottomPanel = new WpfStackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0)
            };

            bottomPanel.Children.Add(SymbolFilterCollapseButton);
            WpfGrid.SetRow(bottomPanel, 2);
            groupGrid.Children.Add(bottomPanel);
        }

        private void DockOperationsButtonToBottom()
        {
            if (SymbolOperationsGroup.Child is not WpfGrid groupGrid)
                return;

            while (groupGrid.RowDefinitions.Count < 3)
                groupGrid.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });

            WpfGrid? headerGrid = null;
            foreach (WpfUIElement child in groupGrid.Children)
            {
                if (child is WpfGrid grid && grid.Children.Contains(SymbolOperationsCollapseButton))
                {
                    headerGrid = grid;
                    break;
                }
            }

            if (headerGrid == null)
                return;

            headerGrid.Children.Remove(SymbolOperationsCollapseButton);

            var bottomPanel = new WpfStackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0)
            };

            bottomPanel.Children.Add(SymbolOperationsCollapseButton);
            WpfGrid.SetRow(bottomPanel, 2);
            groupGrid.Children.Add(bottomPanel);
        }

        private void EnsureClearFiltersButton()
        {
            if (SymbolFilterGroup.Child is not WpfGrid groupGrid)
                return;

            WpfStackPanel? bottomPanel = null;
            foreach (WpfUIElement child in groupGrid.Children)
            {
                if (child is WpfStackPanel panel && panel.Children.Contains(SymbolFilterCollapseButton))
                {
                    bottomPanel = panel;
                    break;
                }
            }

            if (bottomPanel == null)
                return;

            if (_clearSymbolFiltersButton == null)
            {
                _clearSymbolFiltersButton = new WpfButton
                {
                    Content = "پاک کردن همه",
                    Width = 90,
                    Height = 25,
                    Padding = new Thickness(4, 0, 4, 0),
                    Margin = new Thickness(0, 0, 4, 0),
                    ToolTip = "پاک کردن همه فیلترها"
                };
                _clearSymbolFiltersButton.Click += ClearSymbolFiltersButton_Click;
                bottomPanel.Children.Insert(0, _clearSymbolFiltersButton);
            }

            _clearSymbolFiltersButton.Visibility = _symbolFilterGroupCollapsed
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void ClearSymbolFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            ClearFilterControls(SymbolFilterHost);
            ApplySymbolFilter();
        }

        private static void ClearFilterControls(DependencyObject parent)
        {
            for (int i = 0; i < WpfVisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = WpfVisualTreeHelper.GetChild(parent, i);

                switch (child)
                {
                    case WpfTextBox textBox:
                        textBox.Clear();
                        break;
                    case WpfComboBox comboBox:
                        if (comboBox.Items.Count > 0)
                            comboBox.SelectedIndex = 0;
                        break;
                    case WpfCheckBox checkBox:
                        checkBox.IsChecked = false;
                        break;
                    case WpfRadioButton radioButton:
                        radioButton.IsChecked = false;
                        break;
                }

                ClearFilterControls(child);
            }
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
            EnsureClearFiltersButton();
        }

        private void SymbolOperationsCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            _symbolOperationsGroupCollapsed = !_symbolOperationsGroupCollapsed;
            SymbolOperationsContent.Visibility = _symbolOperationsGroupCollapsed ? Visibility.Collapsed : Visibility.Visible;
            SymbolOperationsCollapseButton.Content = _symbolOperationsGroupCollapsed ? "▲" : "▼";
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
