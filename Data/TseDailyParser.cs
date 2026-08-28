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
                try { bars.Add(ParseFields(fields, dataSource, bars.Count)); }
                catch { }
                rowNumber++;
            }
            return bars;
        }

        // Returns the first valid record and the chronologically latest valid record.
        // For files without date/time, physical order is the only available chronology.
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
                    MarketBar bar = ParseSummaryFields(fields, dataSource, index++);
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
                catch { }
                rowNumber++;
            }

            // If dates exist but none could be parsed, fall back to the last valid record.
            if (latestBar == null && firstBar != null)
                latestBar = firstBar;

            return (firstBar, latestBar);
        }

        private static MarketBar ParseSummaryFields(string[] fields, DataSource dataSource, int index)
        {
            var bar = new MarketBar
            {
                Index = index,
                PersianTicker = GetString(fields, dataSource.SymbolColumn),
                EnglishTicker = GetString(fields, dataSource.EnglishTickerColumn),
                Open = GetDouble(fields, dataSource.OpenColumn),
                High = GetDouble(fields, dataSource.HighColumn),
                Low = GetDouble(fields, dataSource.LowColumn),
                Close = GetDouble(fields, dataSource.CloseColumn),
                Volume = GetDouble(fields, dataSource.VolumeColumn),
                TSEClose = GetDouble(fields, dataSource.TSECloseColumn)
            };

            if (dataSource.HasDateTime)
            {
                bar.JalaliDate = GetString(fields, dataSource.DateColumn);
                bar.Time = GetString(fields, dataSource.TimeColumn);
                bar.Timestamp = ParseTimestamp(bar.JalaliDate, bar.Time, dataSource);
            }
            return bar;
        }

        private static MarketBar ParseFields(string[] fields, DataSource dataSource, int index)
        {
            var bar = new MarketBar
            {
                Index = index,
                PersianTicker = GetString(fields, dataSource.SymbolColumn),
                EnglishTicker = GetString(fields, dataSource.EnglishTickerColumn),
                Open = GetDouble(fields, dataSource.OpenColumn),
                High = GetDouble(fields, dataSource.HighColumn),
                Low = GetDouble(fields, dataSource.LowColumn),
                Close = GetDouble(fields, dataSource.CloseColumn),
                Volume = GetDouble(fields, dataSource.VolumeColumn),
                TSEClose = GetDouble(fields, dataSource.TSECloseColumn),
                Previous = GetDouble(fields, dataSource.PreviousColumn),
                Value = GetDouble(fields, dataSource.ValueColumn),
                TradeCount = GetInt(fields, dataSource.TradeCountColumn),
                ShareCount = GetDouble(fields, dataSource.ShareCountColumn),
                MarketValue = GetDouble(fields, dataSource.MarketValueColumn)
            };

            if (dataSource.HasDateTime)
            {
                bar.JalaliDate = GetString(fields, dataSource.DateColumn);
                bar.Time = GetString(fields, dataSource.TimeColumn);
                bar.Timestamp = ParseTimestamp(bar.JalaliDate, bar.Time, dataSource);
            }
            return bar;
        }

        private static string GetString(string[] fields, int index)
        {
            if (index < 0 || index >= fields.Length) return "";
            return fields[index].Trim();
        }

        private static double GetDouble(string[] fields, int index)
        {
            if (index < 0 || index >= fields.Length) return 0;
            string value = fields[index].Trim().Replace(",", "");
            if (string.IsNullOrWhiteSpace(value)) return 0;
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result) ? result : 0;
        }

        private static int GetInt(string[] fields, int index)
        {
            if (index < 0 || index >= fields.Length) return 0;
            string value = fields[index].Trim().Replace(",", "");
            if (string.IsNullOrWhiteSpace(value)) return 0;
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : 0;
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
