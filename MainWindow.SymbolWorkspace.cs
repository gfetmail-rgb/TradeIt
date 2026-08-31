using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TradeIt.Models;
using WpfButton = System.Windows.Controls.Button;
using WpfGrid = System.Windows.Controls.Grid;
using WpfTabControl = System.Windows.Controls.TabControl;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;

namespace TradeIt
{
    public partial class MainWindow
    {
        private WpfTabControl? _symbolToolsTabControl;
        private WpfGrid? _symbolToolsRowHost;
        private bool _symbolToolsExpanded = true;
        private readonly TradeIt.Services.SymbolClassificationStore _classificationStore = new();

        private void BuildSymbolToolsTabs()
        {
            if (_symbolToolsTabControl != null || SymbolsGroupsContent == null)
                return;

            SymbolsGroupsContent.Children.Remove(SymbolFilterGroup);
            SymbolsGroupsContent.Children.Remove(SymbolOperationsGroup);

            _symbolToolsTabControl = new WpfTabControl
            {
                Margin = new Thickness(0),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };

            _symbolToolsTabControl.Items.Add(new TabItem { Header = "فیلترها", Content = SymbolFilterGroup });
            _symbolToolsTabControl.Items.Add(new TabItem { Header = "عملیات", Content = SymbolOperationsGroup });
            _symbolToolsTabControl.Items.Add(new TabItem { Header = "طبقه‌بندی", Content = BuildClassificationPage() });
            _symbolToolsTabControl.Items.Add(new TabItem { Header = "صفحه جدید", Content = new WpfGrid() });
            _symbolToolsTabControl.SelectionChanged += SymbolToolsTabControl_SelectionChanged;

            var host = new WpfGrid();
            _symbolToolsRowHost = host;
            Grid.SetRow(host, 1);
            SymbolsGroupsContent.Children.Add(host);
            host.Children.Add(_symbolToolsTabControl);

            var collapseButton = new WpfButton
            {
                Content = "▲",
                Width = 28,
                Height = 24,
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 4, 0),
                ToolTip = "باز/بسته کردن پنل ابزارهای نماد"
            };
            collapseButton.Click += SymbolToolsCollapseButton_Click;
            Panel.SetZIndex(collapseButton, 10);
            host.Children.Add(collapseButton);

            _symbolToolsExpanded = true;
            UpdateSymbolToolsLayout(collapseButton);
        }

        private void SymbolToolsCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is WpfButton button)
            {
                _symbolToolsExpanded = !_symbolToolsExpanded;
                UpdateSymbolToolsLayout(button);
            }
        }

        private void UpdateSymbolToolsLayout(WpfButton button)
        {
            if (_symbolToolsRowHost == null)
                return;

            if (_symbolToolsExpanded)
            {
                SymbolTableGroupRow.Height = new GridLength(1, GridUnitType.Auto);
                _symbolToolsRowHost.Visibility = Visibility.Visible;
                button.Content = "▲";
            }
            else
            {
                SymbolTableGroupRow.Height = new GridLength(1, GridUnitType.Star);
                _symbolToolsRowHost.Visibility = Visibility.Collapsed;
                button.Content = "▼";
            }
        }

        private FrameworkElement BuildClassificationPage()
        {
            var root = new WpfGrid { Margin = new Thickness(8) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            root.Children.Add(new TextBlock
            {
                Text = "تعیین نوع سهم، صنعت، گروه و زیرگروه",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            });

            var fields = new StackPanel { Orientation = Orientation.Horizontal };
            fields.Children.Add(MakeField("نوع سهم"));
            fields.Children.Add(MakeField("صنعت"));
            fields.Children.Add(MakeField("گروه"));
            fields.Children.Add(MakeField("زیرگروه"));
            Grid.SetRow(fields, 1);
            root.Children.Add(fields);

            var hint = new TextBlock
            {
                Text = "یک یا چند نماد را در لیست انتخاب کنید، مقادیر را وارد کنید و «اعمال طبقه‌بندی» را بزنید.",
                Foreground = Brushes.Gray,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 8)
            };
            Grid.SetRow(hint, 2);
            root.Children.Add(hint);

            var save = new WpfButton
            {
                Content = "اعمال طبقه‌بندی",
                Height = 30,
                Padding = new Thickness(12, 0),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            save.Click += ClassificationApplyButton_Click;
            Grid.SetRow(save, 3);
            root.Children.Add(save);
            return root;
        }

        private WpfTextBox MakeField(string label)
        {
            var panel = new StackPanel { Width = 110, Margin = new Thickness(0, 0, 6, 0) };
            panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 3) });
            var box = new WpfTextBox { Height = 28, Padding = new Thickness(5, 1, 5, 1) };
            panel.Children.Add(box);
            return box;
        }

        private void SymbolToolsTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_symbolToolsTabControl?.SelectedIndex != 2)
                return;

            _classificationStore.ApplyTo(_allSymbols);
            var selected = _allSymbols.FirstOrDefault(x => x.IsSelected);
            if (selected == null || _symbolToolsTabControl.SelectedContent is not FrameworkElement page)
                return;

            var boxes = FindTextBoxes(page).ToArray();
            if (boxes.Length >= 4)
            {
                boxes[0].Text = selected.SecurityType ?? "";
                boxes[1].Text = selected.Industry ?? "";
                boxes[2].Text = selected.Group ?? "";
                boxes[3].Text = selected.SubGroup ?? "";
            }
        }

        private void ClassificationApplyButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = _allSymbols.Where(x => x.IsSelected).ToList();
            if (selected.Count == 0)
            {
                WpfMessageBox.Show("ابتدا حداقل یک نماد را انتخاب کنید.", "طبقه‌بندی", WpfMessageBoxButton.OK, WpfMessageBoxImage.Information);
                return;
            }

            if (_symbolToolsTabControl?.SelectedContent is not FrameworkElement page)
                return;

            var boxes = FindTextBoxes(page).ToArray();
            if (boxes.Length < 4)
                return;

            foreach (SymbolInfo symbol in selected)
            {
                symbol.SecurityType = boxes[0].Text.Trim();
                symbol.Industry = boxes[1].Text.Trim();
                symbol.Group = boxes[2].Text.Trim();
                symbol.SubGroup = boxes[3].Text.Trim();
            }

            _classificationStore.Save(selected);
            StatusTextBlock.Text = $"طبقه‌بندی {selected.Count:N0} نماد اعمال شد.";
        }

        private static IEnumerable<WpfTextBox> FindTextBoxes(DependencyObject root)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is WpfTextBox textBox)
                    yield return textBox;
                foreach (WpfTextBox nested in FindTextBoxes(child))
                    yield return nested;
            }
        }
    }
}
