using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TradeIt.Models;

namespace TradeIt.Data
{
    public class TseDailyParser
    {
        public List<MarketBar> Parse(string filePath, DataSource dataSource)
        {
            EnsureSource(filePath, dataSource);
            var bars = new List<MarketBar>();
            int row = 0;
            foreach (string raw in File.ReadLines(filePath))
            {
                row++;
                string line = raw.Trim();
                if (string.IsNullOrWhiteSpace(line) || (row == 1 && dataSource.HasHeader)) continue;
                string[] f = Split(line, dataSource.Delimiter);
                try { bars.Add(ParseBar(f, dataSource, bars.Count, filePath, true)); }
                catch (Exception ex) { throw new FormatException($"داده ردیف {row} در فایل «{Path.GetFileName(filePath)}» معتبر نیست: {ex.Message}", ex); }
            }
            return bars;
        }

        public (MarketBar? FirstBar, MarketBar? LastBar) ParseSummary(string filePath, DataSource dataSource)
        {
            EnsureSource(filePath, dataSource);
            MarketBar? first = null, last = null;
            DateTime latest = DateTime.MinValue;
            bool hasLatest = false;
            int row = 0, index = 0;
            foreach (string raw in File.ReadLines(filePath))
            {
                row++;
                string line = raw.Trim();
                if (string.IsNullOrWhiteSpace(line) || (row == 1 && dataSource.HasHeader)) continue;
                string[] f = Split(line, dataSource.Delimiter);
                try
                {
                    MarketBar bar = ParseBar(f, dataSource, index++, filePath, false);
                    first ??= bar;
                    if (bar.Timestamp.HasValue)
                    {
                        if (!hasLatest || bar.Timestamp.Value > latest) { latest = bar.Timestamp.Value; last = bar; hasLatest = true; }
                    }
                    else last = bar;
                }
                catch (Exception ex) { throw new FormatException($"داده ردیف {row} در فایل «{Path.GetFileName(filePath)}» معتبر نیست: {ex.Message}", ex); }
            }
            return (first, last);
        }

        private static MarketBar ParseBar(string[] f, DataSource ds, int index, string filePath, bool full)
        {
            double open = RequiredDouble(f, ds.OpenColumn, "قیمت باز شدن");
            double high = RequiredDouble(f, ds.HighColumn, "بیشترین قیمت");
            double low = RequiredDouble(f, ds.LowColumn, "کمترین قیمت");
            double close = RequiredDouble(f, ds.CloseColumn, "قیمت پایانی");

            var bar = new MarketBar
            {
                Index = index,
                PersianTicker = GetSymbol(f, ds, filePath),
                EnglishTicker = OptionalString(f, ds.EnglishTickerColumn),
                Open = open, High = high, Low = low, Close = close,
                Volume = OptionalDouble(f, ds.VolumeColumn, "حجم"),
                TSEClose = OptionalDouble(f, ds.TSECloseColumn, "پایانی بورس"),
                Previous = OptionalDouble(f, ds.PreviousColumn, "قیمت پایانی دیروز"),
                Value = OptionalDouble(f, ds.ValueColumn, "ارزش معاملات"),
                TradeCount = OptionalInt(f, ds.TradeCountColumn, "تعداد معاملات"),
                ShareCount = OptionalDouble(f, ds.ShareCountColumn, "تعداد سهام"),
                MarketValue = OptionalDouble(f, ds.MarketValueColumn, "ارزش بازار"),
                Calendar = ds.HasDateTime ? (ds.Calendar?.Trim() ?? "") : ""
            };

            if (ds.TSECloseColumn < 0) bar.TSEClose = close;

            if (ds.HasDateTime)
            {
                bar.JalaliDate = OptionalString(f, ds.DateColumn);
                bar.Time = OptionalString(f, ds.TimeColumn);
                bar.Timestamp = ParseTimestamp(bar.JalaliDate, bar.Time, ds);
            }
            return bar;
        }

        private static string GetSymbol(string[] f, DataSource ds, string filePath)
        {
            if (string.Equals(ds.SymbolSource, "FileName", StringComparison.OrdinalIgnoreCase))
                return Path.GetFileNameWithoutExtension(filePath)?.Trim() ?? "";
            if (string.Equals(ds.SymbolSource, "FileContent", StringComparison.OrdinalIgnoreCase))
                return RequiredString(f, ds.SymbolColumn, "نام نماد");
            throw new FormatException("منبع نام نماد در تنظیمات داده معتبر نیست.");
        }

        private static string OptionalString(string[] f, int index) => index >= 0 && index < f.Length ? f[index].Trim() : "";

        private static string RequiredString(string[] f, int index, string name)
        {
            if (index < 0 || index >= f.Length)
                throw new FormatException($"ستون «{name}» (شماره {index + 1}) وجود ندارد. تعداد ستون‌های ردیف: {f.Length}.");
            string value = f[index].Trim();
            if (string.IsNullOrWhiteSpace(value))
                throw new FormatException($"مقدار ستون «{name}» خالی است (شماره ستون: {index + 1}).");
            return value;
        }

        private static double RequiredDouble(string[] f, int index, string name)
        {
            if (index < 0 || index >= f.Length)
                throw new FormatException($"ستون عددی ضروری «{name}» (شماره ستون: {index + 1}) وجود ندارد. تعداد ستون‌های ردیف: {f.Length}.");
            return ParseDouble(f[index], index, name, true);
        }

        private static double OptionalDouble(string[] f, int index, string name)
        {
            if (index < 0 || index >= f.Length) return 0;
            string raw = f[index].Trim();
            if (string.IsNullOrWhiteSpace(raw)) return 0;
            return ParseDouble(raw, index, name, false);
        }

        private static double ParseDouble(string raw, int index, string name, bool required)
        {
            string value = raw.Trim().Replace(",", "");
            if (string.IsNullOrWhiteSpace(value))
            {
                if (!required) return 0;
                throw new FormatException($"مقدار ستون عددی «{name}» خالی است (شماره ستون: {index + 1}).");
            }
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result) || double.IsNaN(result) || double.IsInfinity(result))
                throw new FormatException($"مقدار ستون عددی «{name}» معتبر نیست (شماره ستون: {index + 1}). مقدار خام: «{raw}».");
            return result;
        }

        private static int OptionalInt(string[] f, int index, string name)
        {
            if (index < 0 || index >= f.Length) return 0;
            string raw = f[index].Trim().Replace(",", "");
            if (string.IsNullOrWhiteSpace(raw)) return 0;
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                throw new FormatException($"مقدار ستون عدد صحیح «{name}» معتبر نیست (شماره ستون: {index + 1}). مقدار خام: «{raw}».");
            return result;
        }

        private static string[] Split(string line, string delimiter) => line.Split(new[] { delimiter ?? "," }, StringSplitOptions.None);

        private static DateTime? ParseTimestamp(string date, string time, DataSource ds)
        {
            if (string.IsNullOrWhiteSpace(date)) return null;
            string normalized = date.Trim().Replace("/", "-");
            string[] p = normalized.Split('-');
            try
            {
                if (string.Equals(ds.Calendar, "Persian", StringComparison.OrdinalIgnoreCase) && p.Length >= 3 &&
                    int.TryParse(p[0], out int y) && int.TryParse(p[1], out int m) && int.TryParse(p[2], out int d))
                {
                    var pc = new PersianCalendar();
                    DateTime dt = pc.ToDateTime(y, m, d, 0, 0, 0, 0);
                    if (TimeSpan.TryParse(time?.Trim(), CultureInfo.InvariantCulture, out TimeSpan ts)) dt += ts;
                    return dt;
                }
                string combined = string.IsNullOrWhiteSpace(time) ? date : $"{date} {time}";
                if (DateTime.TryParseExact(combined, new[] { ds.DateFormat + " " + ds.TimeFormat, ds.DateFormat }, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTime exact)) return exact;
                if (DateTime.TryParse(combined, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTime result)) return result;
            }
            catch { }
            return null;
        }

        private static void EnsureSource(string filePath, DataSource ds)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("فایل داده بورس پیدا نشد.", filePath);
            if (ds == null) throw new ArgumentNullException(nameof(ds));
        }
    }
}