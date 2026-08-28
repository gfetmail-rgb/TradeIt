
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TradeIt.Models;

namespace TradeIt.Data
{
    public class TseDailyParser
    {
        // =========================================================
        // Parse کامل فایل
        //
        // این متد فقط زمانی استفاده می‌شود که کل تاریخچه برای
        // نمودار لازم باشد.
        // =========================================================

        public List<MarketBar> Parse(
            string filePath,
            DataSource dataSource)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException(
                    "فایل داده بورس پیدا نشد.",
                    filePath);

            if (dataSource == null)
                throw new ArgumentNullException(
                    nameof(dataSource));

            var bars =
                new List<MarketBar>();

            int rowNumber = 0;

            foreach (string rawLine in File.ReadLines(filePath))
            {
                string line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line))
                {
                    rowNumber++;
                    continue;
                }

                if (rowNumber == 0 &&
                    dataSource.HasHeader)
                {
                    rowNumber++;
                    continue;
                }

                string[] fields =
                    line.Split(
                        new[] { dataSource.Delimiter },
                        StringSplitOptions.None);

                try
                {
                    MarketBar bar =
                        ParseFields(
                            fields,
                            dataSource,
                            bars.Count);

                    bars.Add(bar);
                }
                catch
                {
                    // رکورد خراب نادیده گرفته می‌شود.
                }

                rowNumber++;
            }

            return bars;
        }


        // =========================================================
        // Parse Summary
        //
        // این نسخه مخصوص Load لیست نمادهاست.
        //
        // نکته مهم:
        //
        // دیگر OHLC / Volume / TradeCount / MarketValue و غیره
        // برای تمام رکوردهای فایل Parse نمی‌شوند.
        //
        // فقط:
        // - اولین رکورد معتبر
        // - آخرین رکورد معتبر
        //
        // استخراج می‌شود.
        //
        // بنابراین برای Load سبد بسیار سبک‌تر از Parse کامل است.
        // =========================================================

        public (
            MarketBar? FirstBar,
            MarketBar? LastBar)
            ParseSummary(
                string filePath,
                DataSource dataSource)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException(
                    "فایل داده بورس پیدا نشد.",
                    filePath);

            if (dataSource == null)
                throw new ArgumentNullException(
                    nameof(dataSource));


            MarketBar? firstBar = null;
            MarketBar? lastBar = null;


            int rowNumber = 0;
            int index = 0;


            foreach (string rawLine in File.ReadLines(filePath))
            {
                string line = rawLine.Trim();


                if (string.IsNullOrWhiteSpace(line))
                {
                    rowNumber++;
                    continue;
                }


                if (rowNumber == 0 &&
                    dataSource.HasHeader)
                {
                    rowNumber++;
                    continue;
                }


                string[] fields =
                    line.Split(
                        new[] { dataSource.Delimiter },
                        StringSplitOptions.None);


                try
                {
                    // =================================================
                    // برای Summary فقط اطلاعات ضروری را Parse می‌کنیم.
                    // =================================================

                    MarketBar bar =
                        ParseSummaryFields(
                            fields,
                            dataSource,
                            index);


                    if (firstBar == null)
                    {
                        firstBar = bar;
                    }


                    lastBar = bar;

                    index++;
                }
                catch
                {
                    // رکورد خراب نادیده گرفته می‌شود.
                }


                rowNumber++;
            }


            return (
                firstBar,
                lastBar);
        }


        // =========================================================
        // Parse Summary Fields
        //
        // فقط اطلاعات مورد نیاز SymbolDataService
        // =========================================================

        private static MarketBar ParseSummaryFields(
            string[] fields,
            DataSource dataSource,
            int index)
        {
            var bar =
                new MarketBar
                {
                    Index = index,

                    PersianTicker =
                        GetString(
                            fields,
                            dataSource.SymbolColumn),

                    EnglishTicker =
                        GetString(
                            fields,
                            dataSource.EnglishTickerColumn),

                    Open =
                        GetDouble(
                            fields,
                            dataSource.OpenColumn),

                    High =
                        GetDouble(
                            fields,
                            dataSource.HighColumn),

                    Low =
                        GetDouble(
                            fields,
                            dataSource.LowColumn),

                    Close =
                        GetDouble(
                            fields,
                            dataSource.CloseColumn),

                    Volume =
                        GetDouble(
                            fields,
                            dataSource.VolumeColumn),

                    TSEClose =
                        GetDouble(
                            fields,
                            dataSource.TSECloseColumn)
                };


            // =========================================================
            // Date / Time
            // =========================================================

            if (dataSource.HasDateTime)
            {
                bar.JalaliDate =
                    GetString(
                        fields,
                        dataSource.DateColumn);

                bar.Time =
                    GetString(
                        fields,
                        dataSource.TimeColumn);

                bar.Timestamp =
                    ParseTimestamp(
                        bar.JalaliDate,
                        bar.Time,
                        dataSource);
            }


            return bar;
        }


        // =========================================================
        // Parse کامل Fields
        //
        // این بخش برای نمودار است و دست نخورده باقی می‌ماند.
        // =========================================================

        private static MarketBar ParseFields(
            string[] fields,
            DataSource dataSource,
            int index)
        {
            var bar =
                new MarketBar
                {
                    Index = index,

                    PersianTicker =
                        GetString(
                            fields,
                            dataSource.SymbolColumn),

                    EnglishTicker =
                        GetString(
                            fields,
                            dataSource.EnglishTickerColumn),

                    Open =
                        GetDouble(
                            fields,
                            dataSource.OpenColumn),

                    High =
                        GetDouble(
                            fields,
                            dataSource.HighColumn),

                    Low =
                        GetDouble(
                            fields,
                            dataSource.LowColumn),

                    Close =
                        GetDouble(
                            fields,
                            dataSource.CloseColumn),

                    Volume =
                        GetDouble(
                            fields,
                            dataSource.VolumeColumn),

                    TSEClose =
                        GetDouble(
                            fields,
                            dataSource.TSECloseColumn),

                    Previous =
                        GetDouble(
                            fields,
                            dataSource.PreviousColumn),

                    Value =
                        GetDouble(
                            fields,
                            dataSource.ValueColumn),

                    TradeCount =
                        GetInt(
                            fields,
                            dataSource.TradeCountColumn),

                    ShareCount =
                        GetDouble(
                            fields,
                            dataSource.ShareCountColumn),

                    MarketValue =
                        GetDouble(
                            fields,
                            dataSource.MarketValueColumn)
                };


            if (dataSource.HasDateTime)
            {
                bar.JalaliDate =
                    GetString(
                        fields,
                        dataSource.DateColumn);

                bar.Time =
                    GetString(
                        fields,
                        dataSource.TimeColumn);

                bar.Timestamp =
                    ParseTimestamp(
                        bar.JalaliDate,
                        bar.Time,
                        dataSource);
            }


            return bar;
        }


        // =========================================================
        // String
        // =========================================================

        private static string GetString(
            string[] fields,
            int index)
        {
            if (index < 0 ||
                index >= fields.Length)
            {
                return "";
            }

            return fields[index].Trim();
        }


        // =========================================================
        // Double
        // =========================================================

        private static double GetDouble(
            string[] fields,
            int index)
        {
            if (index < 0 ||
                index >= fields.Length)
            {
                return 0;
            }

            string value =
                fields[index].Trim();


            if (string.IsNullOrWhiteSpace(value))
                return 0;


            value =
                value.Replace(",", "");


            if (double.TryParse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double result))
            {
                return result;
            }


            return 0;
        }


        // =========================================================
        // Int
        // =========================================================

        private static int GetInt(
            string[] fields,
            int index)
        {
            if (index < 0 ||
                index >= fields.Length)
            {
                return 0;
            }


            string value =
                fields[index].Trim();


            if (string.IsNullOrWhiteSpace(value))
                return 0;


            value =
                value.Replace(",", "");


            if (int.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int result))
            {
                return result;
            }


            return 0;
        }


        // =========================================================
        // Timestamp
        // =========================================================

        private static DateTime? ParseTimestamp(
            string date,
            string time,
            DataSource dataSource)
        {
            if (string.IsNullOrWhiteSpace(date))
                return null;


            date =
                date.Trim();


            time =
                time?.Trim() ?? "";


            try
            {
                string combined =
                    string.IsNullOrWhiteSpace(time)
                        ? date
                        : $"{date} {time}";


                string format =
                    string.IsNullOrWhiteSpace(time)
                        ? dataSource.DateFormat
                        : $"{dataSource.DateFormat} {dataSource.TimeFormat}";


                // =====================================================
                // Persian Calendar
                // =====================================================

                if (dataSource.Calendar == "Persian")
                {
                    CultureInfo culture =
                        new CultureInfo("fa-IR");


                    culture.DateTimeFormat.Calendar =
                        new PersianCalendar();


                    if (DateTime.TryParseExact(
                            combined,
                            format,
                            culture,
                            DateTimeStyles.None,
                            out DateTime result))
                    {
                        return result;
                    }


                    return null;
                }


                // =====================================================
                // Gregorian Calendar
                // =====================================================

                if (DateTime.TryParseExact(
                        combined,
                        format,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime gregorianResult))
                {
                    return gregorianResult;
                }
            }
            catch
            {
                // تاریخ یا زمان نامعتبر است.
            }


            return null;
        }
    }
}

