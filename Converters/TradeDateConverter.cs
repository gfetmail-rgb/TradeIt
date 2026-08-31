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
            string calendar = "Persian";

            if (values != null)
            {
                foreach (object value in values)
                {
                    if (value is DateTime dt)
                    {
                        date = dt;
                        continue;
                    }

                    if (value is string text)
                    {
                        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
                            date = parsed;
                        else if (text.Equals("Gregorian", StringComparison.OrdinalIgnoreCase) || text.Equals("Persian", StringComparison.OrdinalIgnoreCase))
                            calendar = text;
                    }
                }
            }

            if (!date.HasValue)
                return string.Empty;

            if (calendar.Equals("Gregorian", StringComparison.OrdinalIgnoreCase))
                return date.Value.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);

            PersianCalendar pc = new PersianCalendar();
            string result = string.Format(CultureInfo.InvariantCulture, "{0:0000}/{1:00}/{2:00}", pc.GetYear(date.Value), pc.GetMonth(date.Value), pc.GetDayOfMonth(date.Value));
            return ToPersianDigits(result);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public static string ToPersianDigits(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input.Replace('0', '۰').Replace('1', '۱').Replace('2', '۲').Replace('3', '۳').Replace('4', '۴').Replace('5', '۵').Replace('6', '۶').Replace('7', '۷').Replace('8', '۸').Replace('9', '۹');
        }
    }
}