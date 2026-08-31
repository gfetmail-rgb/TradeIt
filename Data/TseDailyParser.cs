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
                        $"داده ردیف {rowNumber + 1} در فایل «{Path.GetFileName(filePath)}» معتبر نیست: {ex.Message}",
                        ex);
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
                    if (firstBar == null)
                        firstBar = bar;

                    if (dataSource.HasDateTime)
                    {
                        if (bar.Timestamp.HasValue && (!hasTimestamp || bar.Timestamp.Value > latestTimestamp))
                        {
                            latestTimestamp = bar.Timestamp.Value;
                            latestBar = bar;
                            hasTimestamp = true;
                        }
                    }
                    else
                    {
                        latestBar = bar;
                    }
                }
                catch (Exception ex)
                {
                    throw new FormatException(
                        $"داده ردیف {rowNumber + 1} در فایل «{Path.GetFileName(filePath)}» معتبر نیست: {ex.Message}",
                        ex);
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
                Open = GetRequiredDouble(fields, dataSource.OpenColumn),
                High = GetRequiredDouble(fields, dataSource.HighColumn),
                Low = GetRequiredDouble(fields, dataSource.LowColumn),
                Close = GetRequiredDouble(fields, dataSource.CloseColumn),
                Volume = GetRequiredDouble(fields, dataSource.VolumeColumn),
                TSEClose = GetRequiredDouble(fields, dataSource.TSECloseColumn)
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
                Open = GetRequiredDouble(fields, dataSource.OpenColumn),
                High = GetRequiredDouble(fields, dataSource.HighColumn),
                Low = GetRequiredDouble(fields, dataSource.LowColumn),
                Close = GetRequiredDouble(fields, dataSource.CloseColumn),
                Volume = GetRequiredDouble(fields, dataSource.VolumeColumn),
                TSEClose = GetRequiredDouble(fields, dataSource.TSECloseColumn),
                Previous = GetRequiredDouble(fields, dataSource.PreviousColumn),
                Value = GetRequiredDouble(fields, dataSource.ValueColumn),
                TradeCount = GetRequiredInt(fields, dataSource.TradeCountColumn),
                ShareCount = GetRequiredDouble(fields, dataSource.ShareCountColumn),
                MarketValue = GetRequiredDouble(fields, dataSource.MarketValueColumn)
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

        private static string GetOptionalString(string[] fields, int index)
        {
            return GetString(fields, index);
        }

        private static string GetRequiredString(string[] fields, int index, string columnName)
        {
            if (index < 0 || index >= fields.Length)
                throw new FormatException($"ستون «{columnName}» (شماره {index}) در فایل داده وجود ندارد. تعداد ستون‌های ردیف: {fields.Length}.");

            string value = fields[index].Trim();
            if (string.IsNullOrWhiteSpace(value))
                throw new FormatException($"مقدار ستون «{columnName}» در ردیف خالی است (شماره ستون: {index + 1}).");

            return value;
        }

        private static double GetRequiredDouble(string[] fields, int index)
        {
            if (index < 0 || index >= fields.Length)
                throw new FormatException("ستون عددی موردنیاز در فایل داده وجود ندارد.");

            string value = fields[index].Trim().Replace(",", "");
            if (string.IsNullOrWhiteSpace(value))
                throw new FormatException("مقدار عددی موردنیاز در فایل داده خالی است.");

            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result) ||
                double.IsNaN(result) || double.IsInfinity(result))
                throw new FormatException("مقدار عددی فایل داده معتبر نیست.");

            return result;
        }

        private static int GetRequiredInt(string[] fields, int index)
        {
            if (index < 0 || index >= fields.Length)
                throw new FormatException("ستون عدد صحیح موردنیاز در فایل داده وجود ندارد.");

            string value = fields[index].Trim().Replace(",", "");
            if (string.IsNullOrWhiteSpace(value))
                throw new FormatException("مقدار عدد صحیح موردنیاز در فایل داده خالی است.");

            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                throw new FormatException("مقدار عدد صحیح فایل داده معتبر نیست.");

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
                string format = string.IsNullOrWhiteSpace(time) ? dataSource.DateFormat : $"{dataSource.DateFormat} {dataSource.TimeFormat}";

                if (dataSource.Calendar == "Persian")
                {
                    CultureInfo culture = new CultureInfo("fa-IR");
                    culture.DateTimeFormat.Calendar = new PersianCalendar();
                    if (DateTime.TryParseExact(combined, format, culture, DateTimeStyles.None, out DateTime result))
                        return result;
                    return null;
                }

                if (DateTime.TryParseExact(combined, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime gregorianResult))
                    return gregorianResult;
            }
            catch { }
            return null;
        }
    }
}