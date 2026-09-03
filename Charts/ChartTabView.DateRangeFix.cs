using System;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _dateRangeFixRegistered = RegisterDateRangeFix();

        private static bool RegisterDateRangeFix()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(DateRangeFix_Loaded));
            return true;
        }

        private static void DateRangeFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart)
                return;

            chart.Dispatcher.BeginInvoke(
                new Action(chart.ApplyDateAndInitialRangeFix),
                DispatcherPriority.ContextIdle);
        }

        private void ApplyDateAndInitialRangeFix()
        {
            try
            {
                bool changed = NormalizeTimestampsFromJalaliDates();

                if (changed)
                {
                    _initialCandleRangeApplied = false;
                    DrawChart();
                }

                _initialCandleRangeApplied = false;
                ApplyInitialCandleRange();

                ConfigureDisplayDateAxis(Chart);
                Chart.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Chart date/range fix failed: {ex}");
            }
        }

        private bool NormalizeTimestampsFromJalaliDates()
        {
            bool changed = false;
            var calendar = new PersianCalendar();

            foreach (var bar in _bars)
            {
                string date = NormalizeDigits(bar.JalaliDate).Trim();
                if (string.IsNullOrWhiteSpace(date))
                    continue;

                string[] parts = date.Split('/', '-', '.');
                if (parts.Length != 3 ||
                    !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int year) ||
                    !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int month) ||
                    !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int day))
                    continue;

                try
                {
                    DateTime converted = calendar.ToDateTime(year, month, day, 0, 0, 0, 0);
                    string time = NormalizeDigits(bar.Time).Trim();
                    if (!string.IsNullOrWhiteSpace(time) &&
                        TimeSpan.TryParse(time, CultureInfo.InvariantCulture, out TimeSpan timeOfDay))
                    {
                        converted = converted.Date.Add(timeOfDay);
                    }

                    if (!bar.Timestamp.HasValue || bar.Timestamp.Value != converted)
                    {
                        bar.Timestamp = converted;
                        changed = true;
                    }
                }
                catch
                {
                    // Invalid Jalali date is left untouched; data validation remains responsible for it.
                }
            }

            return changed;
        }

        private static string NormalizeDigits(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Replace('۰', '0').Replace('۱', '1').Replace('۲', '2').Replace('۳', '3').Replace('۴', '4')
                .Replace('۵', '5').Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9')
                .Replace('٠', '0').Replace('١', '1').Replace('٢', '2').Replace('٣', '3').Replace('٤', '4')
                .Replace('٥', '5').Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9');
        }
    }
}
