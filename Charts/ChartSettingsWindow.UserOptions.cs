using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TradeIt.Charts
{
    public partial class ChartSettingsWindow
    {
        private CheckBox? _openChartInNewTabCheckBox;
        private bool _chartOpeningOptionAdded;

        protected override void OnContentRendered(System.EventArgs e)
        {
            base.OnContentRendered(e);
            AddChartOpeningOption();
        }

        private void AddChartOpeningOption()
        {
            if (_chartOpeningOptionAdded)
                return;

            StackPanel? stack = FindVisualChildren<StackPanel>(this)
                .FirstOrDefault(x => x.Children.OfType<GroupBox>().Any());

            if (stack == null)
                return;

            var group = new GroupBox
            {
                Header = "نحوه باز شدن نمودار",
                Margin = new Thickness(0, 0, 0, 10)
            };

            var panel = new StackPanel { Margin = new Thickness(10) };

            _openChartInNewTabCheckBox = new CheckBox
            {
                Content = "کلیک روی نام سهم در تب جدید باز شود",
                IsChecked = Settings.OpenChartInNewTab,
                Margin = new Thickness(0, 0, 0, 5)
            };

            panel.Children.Add(_openChartInNewTabCheckBox);
            panel.Children.Add(new TextBlock
            {
                Text = "اگر خاموش باشد، همه سهم‌ها در یک تب مشترک نمایش داده می‌شوند.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.Gray
            });

            group.Content = panel;
            stack.Children.Add(group);

            foreach (Button button in FindVisualChildren<Button>(this))
            {
                if (button.Content?.ToString() == "ذخیره")
                {
                    button.PreviewMouseLeftButtonDown += SaveChartOpeningOption;
                    break;
                }
            }

            _chartOpeningOptionAdded = true;
        }

        private void SaveChartOpeningOption(object? sender, MouseButtonEventArgs e)
        {
            if (_openChartInNewTabCheckBox != null)
                Settings.OpenChartInNewTab = _openChartInNewTabCheckBox.IsChecked == true;
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject dependencyObject)
            where T : DependencyObject
        {
            if (dependencyObject == null)
                yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dependencyObject); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(dependencyObject, i);

                if (child is T typedChild)
                    yield return typedChild;

                foreach (T descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }
    }
}