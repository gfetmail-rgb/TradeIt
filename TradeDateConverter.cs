using System;
using System.Globalization;
using System.Windows.Data;

namespace TradeIt
{
    public sealed class TradeDateConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0 || values[0] == null || values[0] == DependencyProperty.UnsetValue)
                return "";

            DateTime date;
            if (values[0] is DateTime dt)
                date = dt;
            else if (values[0] is DateTime? ndt && ndt.HasValue)
                date = ndt.Value;
            else if (!DateTime.TryParse(values[0].ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                return "";

            string calendar = values.Length > 1 && values[1] != null && values[1] != DependencyProperty.UnsetValue
                ? values[1].ToString() ?? ""
                : "";

            if (calendar.Contains("شمسی", StringComparison.OrdinalIgnoreCase) || calendar.Contains("Persian", StringComparison.OrdinalIgnoreCase) || calendar.Contains("Solar", StringComparison.OrdinalIgnoreCase))
            {
                PersianCalendar pc = new PersianCalendar();
                return $"{pc.GetYear(date):0000}/{pc.GetMonth(date):00}/{pc.GetDayOfMonth(date):00}";
            }

            return date.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}