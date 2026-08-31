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
            if (!File.Exists(filePath))
                throw new FileNotFoundException("فایل داده بورس پیدا نشد.", filePath);
            if (dataSource == null)
                throw new ArgumentNullException(nameof(dataSource));

            var bars = new List<MarketBar>();
            int rowNumber = 0;
            foreach (string rawLine in File.ReadLines(filePath))
            {
                string line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line)) { rowNumber++; continue; }
                if (rowNumber == 0 && dataSource.HasHeader) { rowNumber++; continue; }

                string[] fields = line.Split(new[] { dataSource.Delimiter }, StringSplitOptions.None);
                try
                {
                    bars.Add(ParseFields(fields, dataSource, bars.Count, filePath));
                }
                catch (Exception ex)
                {
                    throw new FormatException(
                        $"داده ردیف {rowNumber + 1} در فایل «{Path.GetFileName(filePath)}» معتبر نیست: {ex.Message}", ex);
                }
                rowNumber++;
            }
            return bars;
        }

        public (MarketBar? FirstBar, MarketBar? LastBar) ParseSummary(string filePath, DataSource dataSource)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("فایل داده بورس پیدا نشد.", filePath);
            if (dataSource == null)
                throw new ArgumentNullException(nameof(dataSource));

            MarketBar? firstBar = null;
            MarketBar? latestBar = null;
            DateTime latestTimestamp = DateTime.MinValue;
            bool hasTimestamp = false;
            int rowNumber = 0;
            int index = 0;

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
                    if (dataSource.HasDateTime)
                    {
                        if (bar.Timestamp.HasValue && (!hasTimestamp || bar.Timestamp.Value > latestTimestamp))
                        {
                            latestTimestamp = bar.Timestamp.Value;
                            latestBar = bar;
                            hasTimestamp = true;
                        }
                    }
                    else latestBar = bar;
                }
                catch (Exception ex)
                {
                    throw new FormatException(
                        $"داده ردیف {rowNumber + 1} در فایل «{Path.GetFileName(filePath)}» معتبر نیست: {ex.Message}", ex);
                }
                rowNumber++;
            }
            return (firstBar, latestBar);
        }

        private static MarketBar ParseSummaryFields(string[] fields, DataSource dataSource, int index, string filePath)
        {
            var bar = new MarketBar
            {
                Index = index,
                PersianTicker = GetSymbol(fields, dataSource, filePath),
                EnglishTicker = GetOptionalString(fields, dataSource.EnglishTickerColumn),
                Open = GetRequiredDouble(fields, dataSource.OpenColumn, "قیمت باز شدن"),
                High = GetRequiredDouble(fields, dataSource.HighColumn, "بیشترین قیمت"),
                Low = GetRequiredDouble(fields, dataSource.LowColumn, "کمترین قیمت"),
                Close = GetRequiredDouble(fields, dataSource.CloseColumn, "قیمت پایانی"),
                Volume = GetRequiredDouble(fields, dataSource.VolumeColumn, "حجم"),
                TSEClose = GetRequiredDouble(fields, dataSource.TSECloseColumn, "پایانی بورس")
            };

            if (dataSource.HasDateTime)
            {
                bar.JalaliDate = GetString(fields, dataSource.DateColumn);
                bar.Time = GetString(fields, dataSource.TimeColumn);
                bar.Timestamp = ParseTimestamp(bar.JalaliDate, bar.Time, dataSource);
            }
            return bar;
        }

        private static MarketBar ParseFields(string[] fields, DataSource dataSource, int index, string filePath)
        {
            var bar = new MarketBar
            {
                Index = index,
                PersianTicker = GetSymbol(fields, dataSource, filePath),
                EnglishTicker = GetOptionalString(fields, dataSource.EnglishTickerColumn),
                Open = GetRequiredDouble(fields, dataSource.OpenColumn, "قیمت باز شدن"),
                High = GetRequiredDouble(fields, dataSource.HighColumn, "بیشترین قیمت"),
                Low = GetRequiredDouble(fields, dataSource.LowColumn, "کمترین قیمت"),
                Close = GetRequiredDouble(fields, dataSource.CloseColumn, "قیمت پایانی"),
                Volume = GetRequiredDouble(fields, dataSource.VolumeColumn, "حجم"),
                TSEClose = GetRequiredDouble(fields, dataSource.TSECloseColumn, "پایانی بورس"),
                Previous = GetRequiredDouble(fields, dataSource.PreviousColumn, "قیمت پایانی دیروز"),
                Value = GetRequiredDouble(fields, dataSource.ValueColumn, "ارزش معاملات"),
                TradeCount = GetRequiredInt(fields, dataSource.TradeCountColumn, "تعداد معاملات"),
                ShareCount = GetRequiredDouble(fields, dataSource.ShareCountColumn, "تعداد سهام"),
                MarketValue = GetRequiredDouble(fields, dataSource.MarketValueColumn, "ارزش بازار")
            };

            if (dataSource.HasDateTime)
            {
                bar.JalaliDate = GetString(fields, dataSource.DateColumn);
                bar.Time = GetString(fields, dataSource.TimeColumn);
                bar.Timestamp = ParseTimestamp(bar.JalaliDate, bar.Time, dataSource);
            }
            return bar;
        }

        private static string GetSymbol(string[] fields, DataSource dataSource, string filePath)
        {
            if (string.Equals(dataSource.SymbolSource, "FileName", StringComparison.OrdinalIgnoreCase))
            {
                string symbol = Path.GetFileNameWithoutExtension(filePath)?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(symbol))
                    throw new FormatException("نام نماد از نام فایل قابل استخراج نیست.");
                return symbol;
            }
            if (string.Equals(dataSource.SymbolSource, "FileContent", StringComparison.OrdinalIgnoreCase))
                return GetRequiredString(fields, dataSource.SymbolColumn, "نام نماد");
            throw new FormatException("منبع نام نماد در تنظیمات داده معتبر نیست.");
        }

        private static string GetString(string[] fields, int index)
        {
            if (index < 0 || index >= fields.Length) return "";
            return fields[index].Trim();
        }

        private static string GetOptionalString(string[] fields, int index) => GetString(fields, index);

        private static string GetRequiredString(string[] fields, int index, string columnName)
        {
            if (index < 0 || index >= fields.Length)
                throw new FormatException($"ستون «{columnName}» (شماره {index + 1}) در فایل داده وجود ندارد. تعداد ستون‌های ردیف: {fields.Length}.");
            string value = fields[index].Trim();
            if (string.IsNullOrWhiteSpace(value))
                throw new FormatException($"مقدار ستون «{columnName}» در ردیف خالی است (شماره ستون: {index + 1}).");
            return value;
        }

        private static double GetRequiredDouble(string[] fields, int index, string columnName)
        {
            if (index < 0 || index >= fields.Length)
                throw new FormatException($"ستون عددی «{columnName}» (شماره {index + 1}) در فایل داده وجود ندارد. تعداد ستون‌های ردیف: {fields.Length}.");

            string raw = fields[index].Trim();
            string value = raw.Replace(",", "");
            if (string.IsNullOrWhiteSpace(value))
                throw new FormatException($"مقدار ستون عددی «{columnName}» خالی است (شماره ستون: {index + 1}). مقدار خام: «{raw}».");

            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result) ||
                double.IsNaN(result) || double.IsInfinity(result))
                throw new FormatException($"مقدار ستون عددی «{columnName}» معتبر نیست (شماره ستون: {index + 1}). مقدار خام: «{raw}».");

            return result;
        }

        private static int GetRequiredInt(string[] fields, int index, string columnName)
        {
            if (index < 0 || index >= fields.Length)
                throw new FormatException($"ستون عدد صحیح «{columnName}» (شماره {index + 1}) در فایل داده وجود ندارد. تعداد ستون‌های ردیف: {fields.Length}.");

            string raw = fields[index].Trim();
            string value = raw.Replace(",", "");
            if (string.IsNullOrWhiteSpace(value))
                throw new FormatException($"مقدار ستون عدد صحیح «{columnName}» خالی است (شماره ستون: {index + 1}). مقدار خام: «{raw}».");

            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                throw new FormatException($"مقدار ستون عدد صحیح «{columnName}» معتبر نیست (شماره ستون: {index + 1}). مقدار خام: «{raw}».");
            return result;
        }

        private static DateTime? ParseTimestamp(string date, string time, DataSource dataSource)
        {
            if (string.IsNullOrWhiteSpace(date)) return null;
            date = date.Trim();
            time = time?.Trim() ?? "";
            try
            {
                string combined = string.IsNullOrWhiteSpace(time) ? date : $"{date} {time}";
                if (dataSource.Calendar == CalendarType.Persian)
                {
                    string normalized = date.Replace("/", "-");
                    string[] parts = normalized.Split('-');
                    if (parts.Length >= 3 && int.TryParse(parts[0], out int y) && int.TryParse(parts[1], out int m) && int.TryParse(parts[2], out int d))
                    {
                        var pc = new System.Globalization.PersianCalendar();
                        DateTime dt = pc.ToDateTime(y, m, d, 0, 0, 0, 0);
                        if (!string.IsNullOrWhiteSpace(time) && TimeSpan.TryParse(time, CultureInfo.InvariantCulture, out TimeSpan ts))
                            dt = dt.Date + ts;
                        return dt;
                    }
                }
                if (DateTime.TryParse(combined, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTime result))
                    return result;
                if (DateTime.TryParse(combined, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out result))
                    return result;
            }
            catch { }
            return null;
        }
    }
}
