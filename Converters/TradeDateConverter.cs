using System;
using System.Globalization;
using System.Windows.Data;

namespace TradeIt
{
    public class TradeDateConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime? date = null;
            string sourceDate = "";
            string calendar = "Persian";

            if (values != null)
            {
                foreach (object value in values)
                {
                    if (value is DateTime dt)
                    {
                        date = dt;
                    }
                    else if (value is string text)
                    {
                        if (text.Equals("Gregorian", StringComparison.OrdinalIgnoreCase) || text.Equals("Persian", StringComparison.OrdinalIgnoreCase))
                            calendar = text;
                        else if (string.IsNullOrWhiteSpace(sourceDate))
                            sourceDate = text.Trim();
                    }
                }
            }

            if (calendar.Equals("Persian", StringComparison.OrdinalIgnoreCase) && TryFormatJalaliSource(sourceDate, out string jalali))
                return ToPersianDigits(jalali);

            if (!date.HasValue)
                return string.Empty;

            if (calendar.Equals("Gregorian", StringComparison.OrdinalIgnoreCase))
                return date.Value.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);

            PersianCalendar pc = new PersianCalendar();
            string result = string.Format(CultureInfo.InvariantCulture, "{0:0000}/{1:00}/{2:00}", pc.GetYear(date.Value), pc.GetMonth(date.Value), pc.GetDayOfMonth(date.Value));
            return ToPersianDigits(result);
        }

        private static bool TryFormatJalaliSource(string source, out string result)
        {
            result = "";
            if (string.IsNullOrWhiteSpace(source)) return false;
            string normalized = source.Trim().Replace('-', '/');
            string[] p = normalized.Split('/');
            if (p.Length < 3) return false;
            if (!int.TryParse(p[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y) ||
                !int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int m) ||
                !int.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int d)) return false;
            if (y < 1200 || y > 1600 || m < 1 || m > 12 || d < 1 || d > 31) return false;
            result = string.Format(CultureInfo.InvariantCulture, "{0:0000}/{1:00}/{2:00}", y, m, d);
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();

        public static string ToPersianDigits(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input.Replace('0', '۰').Replace('1', '۱').Replace('2', '۲').Replace('3', '۳').Replace('4', '۴').Replace('5', '۵').Replace('6', '۶').Replace('7', '۷').Replace('8', '۸').Replace('9', '۹');
        }
    }
}