using System;
using System.Globalization;
using System.Windows.Data;

namespace TradeIt
{
    public class TradeDateConverter : IMultiValueConverter
    {
        // =========================================================
        // Convert
        // =========================================================

        public object Convert(
            object[] values,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (values == null ||
                values.Length == 0)
            {
                return string.Empty;
            }

            DateTime? date = null;

            foreach (object value in values)
            {
                if (value == null ||
                    value == DBNull.Value)
                {
                    continue;
                }

                if (value is DateTime dt)
                {
                    date = dt;
                    break;
                }

                if (value is DateTime?)
                {
                    DateTime? nullableDate =
                        (DateTime?)value;

                    if (nullableDate.HasValue)
                    {
                        date = nullableDate.Value;
                        break;
                    }
                }

                if (value is string text &&
                    DateTime.TryParse(
                        text,
                        out DateTime parsed))
                {
                    date = parsed;
                    break;
                }
            }

            if (!date.HasValue)
            {
                return string.Empty;
            }

            string result =
                date.Value.ToString(
                    "yyyy/MM/dd",
                    CultureInfo.InvariantCulture);

            return ToPersianDigits(result);
        }

        // =========================================================
        // ConvertBack
        // =========================================================

        public object[] ConvertBack(
            object value,
            Type[] targetTypes,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException(
                "TradeDateConverter فقط برای نمایش تاریخ استفاده می‌شود.");
        }

        // =========================================================
        // Persian Digits
        // =========================================================

        public static string ToPersianDigits(
            string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return input
                .Replace('0', '۰')
                .Replace('1', '۱')
                .Replace('2', '۲')
                .Replace('3', '۳')
                .Replace('4', '۴')
                .Replace('5', '۵')
                .Replace('6', '۶')
                .Replace('7', '۷')
                .Replace('8', '۸')
                .Replace('9', '۹');
        }
    }
}