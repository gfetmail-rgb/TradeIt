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
            if (!File.Exists(filePath)) throw new FileNotFoundException("فایل داده بورس پیدا نشد.", filePath);
            if (dataSource == null) throw new ArgumentNullException(nameof(dataSource));
            var bars = new List<MarketBar>();
            int rowNumber = 0;
            foreach (string rawLine in File.ReadLines(filePath))
            {
                string line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line)) { rowNumber++; continue; }
                if (rowNumber == 0 && dataSource.HasHeader) { rowNumber++; continue; }
                string[] fields = line.Split(new[] { dataSource.Delimiter }, StringSplitOptions.None);
                try { bars.Add(ParseFields(fields, dataSource, bars.Count, filePath)); }
                catch (Exception ex) { throw new FormatException($"داده ردیف {rowNumber + 1} در فایل «{Path.GetFileName(filePath)}» معتبر نیست: {ex.Message}", ex); }
                rowNumber++;
            }
            return bars;
        }

        public (MarketBar? FirstBar, MarketBar? LastBar) ParseSummary(string filePath, DataSource dataSource)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("فایل داده بورس پیدا نشد.", filePath);
            if (dataSource == null) throw new ArgumentNullException(nameof(dataSource));
            MarketBar? firstBar = null, latestBar = null;
            DateTime latestTimestamp = DateTime.MinValue;
            bool hasTimestamp = false;
            int rowNumber = 0, index = 0;
            foreach (string rawLine in File.ReadLines(filePath))
            {
                string line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line)) { rowNumber++; continue; }
                if (rowNumber == 0 && dataSource.HasHeader) { rowNumber++; continue; }
                string[] fields = line.Split(new[] { dataSource.Delimiter }, StringSplitOptions.None);
                try
                {
                    MarketBar bar = ParseSummaryFields(fields, dataSource, index++, filePath);
                    if (firstBar == null) firstBar = bar;
                    if (dataSource.HasDateTime && bar.Timestamp.HasValue)
                    {
                        if (!hasTimestamp || bar.Timestamp.Value > latestTimestamp) { latestTimestamp = bar.Timestamp.Value; latestBar = bar; hasTimestamp = true; }
                    }
                    else if (!dataSource.HasDateTime) latestBar = bar;
                }
                catch (Exception ex) { throw new FormatException($"داده ردیف {rowNumber + 1} در فایل «{Path.GetFileName(filePath)}» معتبر نیست: {ex.Message}", ex); }
                rowNumber++;
            }
            return (firstBar, latestBar);
        }

        private static MarketBar ParseSummaryFields(string[] fields, DataSource ds, int index, string filePath)
        {
            var bar = new MarketBar {
                Index = index, PersianTicker = GetSymbol(fields, ds, filePath),
                EnglishTicker = GetOptionalString(fields, ds.EnglishTickerColumn),
                Open = GetRequiredDouble(fields, ds.OpenColumn, "قیمت باز شدن"), High = GetRequiredDouble(fields, ds.HighColumn, "بیشترین قیمت"),
                Low = GetRequiredDouble(fields, ds.LowColumn, "کمترین قیمت"), Close = GetRequiredDouble(fields, ds.CloseColumn, "قیمت پایانی"),
                Volume = GetRequiredDouble(fields, ds.VolumeColumn, "حجم"), TSEClose = GetRequiredDouble(fields, ds.TSECloseColumn, "پایانی بورس")
            };
            SetDateTime(bar, fields, ds);
            return bar;
        }

        private static MarketBar ParseFields(string[] fields, DataSource ds, int index, string filePath)
        {
            var bar = new MarketBar {
                Index = index, PersianTicker = GetSymbol(fields, ds, filePath),
                EnglishTicker = GetOptionalString(fields, ds.EnglishTickerColumn),
                Open = GetRequiredDouble(fields, ds.OpenColumn, "قیمت باز شدن"), High = GetRequiredDouble(fields, ds.HighColumn, "بیشترین قیمت"),
                Low = GetRequiredDouble(fields, ds.LowColumn, "کمترین قیمت"), Close = GetRequiredDouble(fields, ds.CloseColumn, "قیمت پایانی"),
                Volume = GetRequiredDouble(fields, ds.VolumeColumn, "حجم"), TSEClose = GetRequiredDouble(fields, ds.TSECloseColumn, "پایانی بورس"),
                Previous = GetRequiredDouble(fields, ds.PreviousColumn, "قیمت پایانی دیروز"), Value = GetRequiredDouble(fields, ds.ValueColumn, "ارزش معاملات"),
                TradeCount = GetRequiredInt(fields, ds.TradeCountColumn, "تعداد معاملات"), ShareCount = GetRequiredDouble(fields, ds.ShareCountColumn, "تعداد سهام"),
                MarketValue = GetRequiredDouble(fields, ds.MarketValueColumn, "ارزش بازار")
            };
            SetDateTime(bar, fields, ds);
            return bar;
        }

        private static void SetDateTime(MarketBar bar, string[] fields, DataSource ds)
        {
            if (!ds.HasDateTime) return;
            bar.JalaliDate = GetString(fields, ds.DateColumn);
            bar.Time = GetString(fields, ds.TimeColumn);
            bar.Timestamp = ParseTimestamp(bar.JalaliDate, bar.Time, ds);
        }

        private static string GetSymbol(string[] fields, DataSource ds, string filePath)
        {
            if (string.Equals(ds.SymbolSource, "FileName", StringComparison.OrdinalIgnoreCase))
            {
                string symbol = Path.GetFileNameWithoutExtension(filePath)?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(symbol)) throw new FormatException("نام نماد از نام فایل قابل استخراج نیست.");
                return symbol;
            }
            if (string.Equals(ds.SymbolSource, "FileContent", StringComparison.OrdinalIgnoreCase)) return GetRequiredString(fields, ds.SymbolColumn, "نام نماد");
            throw new FormatException("منبع نام نماد در تنظیمات داده معتبر نیست.");
        }

        private static string GetString(string[] fields, int index) => index < 0 || index >= fields.Length ? "" : fields[index].Trim();
        private static string GetOptionalString(string[] fields, int index) => GetString(fields, index);

        private static string GetRequiredString(string[] fields, int index, string name)
        {
            if (index < 0 || index >= fields.Length) throw new FormatException($"ستون «{name}» (شماره {index + 1}) در فایل داده وجود ندارد. تعداد ستون‌های ردیف: {fields.Length}.");
            string value = fields[index].Trim();
            if (string.IsNullOrWhiteSpace(value)) throw new FormatException($"مقدار ستون «{name}» در ردیف خالی است (شماره ستون: {index + 1}).");
            return value;
        }

        private static double GetRequiredDouble(string[] fields, int index, string name)
        {
            if (index < 0 || index >= fields.Length) throw new FormatException($"ستون عددی «{name}» (شماره {index + 1}) در فایل داده وجود ندارد. تعداد ستون‌های ردیف: {fields.Length}.");
            string raw = fields[index].Trim();
            if (string.IsNullOrWhiteSpace(raw)) throw new FormatException($"مقدار ستون عددی «{name}» خالی است (شماره ستون: {index + 1}). مقدار خام: «{raw}».");
            string value = raw.Replace(",", "");
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result) || double.IsNaN(result) || double.IsInfinity(result))
                throw new FormatException($"مقدار ستون عددی «{name}» معتبر نیست (شماره ستون: {index + 1}). مقدار خام: «{raw}».");
            return result;
        }

        private static int GetRequiredInt(string[] fields, int index, string name)
        {
            if (index < 0 || index >= fields.Length) throw new FormatException($"ستون عدد صحیح «{name}» (شماره {index + 1}) در فایل داده وجود ندارد. تعداد ستون‌های ردیف: {fields.Length}.");
            string raw = fields[index].Trim();
            if (string.IsNullOrWhiteSpace(raw)) throw new FormatException($"مقدار ستون عدد صحیح «{name}» خالی است (شماره ستون: {index + 1}). مقدار خام: «{raw}».");
            if (!int.TryParse(raw.Replace(",", ""), NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)) throw new FormatException($"مقدار ستون عدد صحیح «{name}» معتبر نیست (شماره ستون: {index + 1}). مقدار خام: «{raw}».");
            return result;
        }

        private static DateTime? ParseTimestamp(string date, string time, DataSource ds)
        {
            if (string.IsNullOrWhiteSpace(date)) return null;
            try
            {
                string normalized = date.Trim().Replace("/", "-");
                string[] p = normalized.Split('-');
                if (ds.Calendar.ToString().Equals("Persian", StringComparison.OrdinalIgnoreCase) && p.Length >= 3 && int.TryParse(p[0], out int y) && int.TryParse(p[1], out int m) && int.TryParse(p[2], out int d))
                {
                    var pc = new PersianCalendar();
                    DateTime dt = pc.ToDateTime(y, m, d, 0, 0, 0, 0);
                    if (TimeSpan.TryParse(time?.Trim(), CultureInfo.InvariantCulture, out TimeSpan ts)) dt = dt.Date + ts;
                    return dt;
                }
                string combined = string.IsNullOrWhiteSpace(time) ? date : $"{date} {time}";
                if (DateTime.TryParse(combined, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTime result)) return result;
                if (DateTime.TryParse(combined, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out result)) return result;
            }
            catch { }
            return null;
        }
    }
}
