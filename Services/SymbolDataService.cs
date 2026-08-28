using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using TradeIt.Data;
using TradeIt.Models;
using TradeIt.Portfolios;

namespace TradeIt.Services
{
    public class SymbolDataService
    {
        // =========================================================
        // Constructor
        // =========================================================

        public SymbolDataService()
        {
        }


        // =========================================================
        // Get Symbols
        // =========================================================

        public List<SymbolInfo> GetSymbols(
            Portfolio portfolio)
        {
            return GetSymbols(
                portfolio,
                CancellationToken.None);
        }


        // =========================================================
        // Get Symbols + Cancellation
        // =========================================================

        public List<SymbolInfo> GetSymbols(
            Portfolio portfolio,
            CancellationToken cancellationToken)
        {
            if (portfolio == null)
            {
                throw new ArgumentNullException(
                    nameof(portfolio));
            }

            if (portfolio.DataSource == null)
            {
                return new List<SymbolInfo>();
            }

            string dataPath =
                portfolio.DataSource.Path ?? "";

            if (string.IsNullOrWhiteSpace(dataPath))
            {
                return new List<SymbolInfo>();
            }


            // =====================================================
            // مهم:
            //
            // اگر سبد Explicit باشد، نباید کل پوشه را بخوانیم.
            //
            // قبلاً:
            //
            //   کل پوشه
            //       ↓
            //   ParseSummary برای همه فایل‌ها
            //       ↓
            //   فیلتر کردن نمادهای سبد
            //
            // اکنون:
            //
            //   لیست فایل‌های خود سبد
            //       ↓
            //   فقط همان فایل‌ها
            //
            // این تغییر اصلی برای رفع کندی باز شدن سبد است.
            // =====================================================

            List<SymbolInfo> result;

            if (portfolio.DataSource.SourceType == "Folder")
            {
                if (portfolio.UseExplicitSymbolList)
                {
                    result =
                        GetExplicitSymbolsFromFolder(
                            portfolio,
                            dataPath,
                            portfolio.DataSource,
                            cancellationToken);
                }
                else
                {
                    result =
                        GetSymbolsFromFolder(
                            dataPath,
                            portfolio.DataSource,
                            cancellationToken);
                }
            }
            else if (portfolio.DataSource.SourceType == "File")
            {
                cancellationToken.ThrowIfCancellationRequested();

                result =
                    GetSymbolsFromFile(
                        dataPath,
                        portfolio.DataSource,
                        cancellationToken);
            }
            else
            {
                return new List<SymbolInfo>();
            }


            // =====================================================
            // Row Number
            // =====================================================

            for (int i = 0;
                 i < result.Count;
                 i++)
            {
                result[i].RowNumber =
                    i + 1;

                result[i].IsSelected =
                    false;
            }

            return result;
        }


        // =========================================================
        // Explicit Symbols From Folder
        //
        // فقط فایل‌هایی را می‌خواند که در خود سبد وجود دارند.
        // =========================================================

        private List<SymbolInfo> GetExplicitSymbolsFromFolder(
            Portfolio portfolio,
            string folderPath,
            DataSource dataSource,
            CancellationToken cancellationToken)
        {
            if (!Directory.Exists(folderPath))
            {
                return new List<SymbolInfo>();
            }

            if (portfolio.Symbols == null ||
                portfolio.Symbols.Count == 0)
            {
                return new List<SymbolInfo>();
            }


            // =====================================================
            // مسیرهای مورد نیاز سبد
            // =====================================================

            List<string> paths =
                portfolio.Symbols
                    .Where(
                        x =>
                            x != null &&
                            !string.IsNullOrWhiteSpace(
                                x.FilePath))
                    .Select(
                        x =>
                            NormalizePath(
                                x.FilePath))
                    .Where(
                        x =>
                            !string.IsNullOrWhiteSpace(x))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();


            if (paths.Count == 0)
            {
                return new List<SymbolInfo>();
            }


            var parsed =
                new ConcurrentBag<SymbolInfo>();


            int workerCount =
                Math.Max(
                    1,
                    Math.Min(
                        Environment.ProcessorCount,
                        4));


            ParallelOptions options =
                new ParallelOptions
                {
                    MaxDegreeOfParallelism =
                        workerCount,

                    CancellationToken =
                        cancellationToken
                };


            // =====================================================
            // فقط فایل‌های خود سبد
            // =====================================================

            Parallel.ForEach(
                paths,
                options,
                filePath =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        if (!File.Exists(filePath))
                        {
                            return;
                        }

                        SymbolInfo? info =
                            CreateSymbolInfoFromFile(
                                filePath,
                                dataSource,
                                cancellationToken);

                        if (info != null)
                        {
                            parsed.Add(info);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch
                    {
                        // فایل نامعتبر نادیده گرفته می‌شود.
                    }
                });


            cancellationToken.ThrowIfCancellationRequested();


            // =====================================================
            // ترتیب همان ترتیب نام نماد
            // =====================================================

            return parsed
                .OrderBy(
                    x =>
                        x.Symbol,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
        }


        // =========================================================
        // Async
        // =========================================================

        public Task<List<SymbolInfo>> GetSymbolsAsync(
            Portfolio portfolio,
            CancellationToken cancellationToken = default)
        {
            return Task.Run(
                () =>
                    GetSymbols(
                        portfolio,
                        cancellationToken),
                cancellationToken);
        }


        // =========================================================
        // Folder - Full Universe
        //
        // فقط زمانی استفاده می‌شود که Explicit نیست.
        // =========================================================

        private List<SymbolInfo> GetSymbolsFromFolder(
            string folderPath,
            DataSource dataSource,
            CancellationToken cancellationToken)
        {
            if (!Directory.Exists(folderPath))
            {
                return new List<SymbolInfo>();
            }

            cancellationToken.ThrowIfCancellationRequested();

            string[] files =
                Directory
                    .EnumerateFiles(
                        folderPath,
                        "*.*",
                        SearchOption.TopDirectoryOnly)
                    .Where(
                        IsDataFile)
                    .ToArray();

            if (files.Length == 0)
            {
                return new List<SymbolInfo>();
            }

            var parsed =
                new ConcurrentBag<SymbolInfo>();

            int workerCount =
                Math.Max(
                    2,
                    Math.Min(
                        Environment.ProcessorCount,
                        8));

            ParallelOptions options =
                new ParallelOptions
                {
                    MaxDegreeOfParallelism =
                        workerCount,

                    CancellationToken =
                        cancellationToken
                };

            Parallel.ForEach(
                files,
                options,
                file =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        SymbolInfo? info =
                            CreateSymbolInfoFromFile(
                                file,
                                dataSource,
                                cancellationToken);

                        if (info != null)
                        {
                            parsed.Add(info);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch
                    {
                        // فایل نامعتبر نادیده گرفته می‌شود.
                    }
                });

            cancellationToken.ThrowIfCancellationRequested();

            return parsed
                .OrderBy(
                    x =>
                        x.Symbol,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
        }


        // =========================================================
        // Create Symbol Info
        // =========================================================

        private SymbolInfo? CreateSymbolInfoFromFile(
            string filePath,
            DataSource dataSource,
            CancellationToken cancellationToken)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();


            // =====================================================
            // Symbol From File Name
            // =====================================================

            if (dataSource.SymbolSource == "FileName")
            {
                string fileSymbol =
                    Path.GetFileNameWithoutExtension(
                        filePath);

                var symbolInfo =
                    new SymbolInfo
                    {
                        Symbol =
                            fileSymbol,

                        DisplayName =
                            fileSymbol,

                        FilePath =
                            filePath,

                        IsSelected =
                            false
                    };

                EnrichSymbolInfoFromSummary(
                    symbolInfo,
                    filePath,
                    dataSource,
                    cancellationToken);

                return symbolInfo;
            }


            // =====================================================
            // Symbol From File Content
            // =====================================================

            (
                MarketBar? FirstBar,
                MarketBar? LastBar)
                summary =
                    ParseSummary(
                        filePath,
                        dataSource,
                        cancellationToken);


            if (summary.FirstBar == null &&
                summary.LastBar == null)
            {
                return null;
            }


            MarketBar? firstBar =
                summary.FirstBar;

            MarketBar? lastBar =
                summary.LastBar;


            string contentSymbol =
                "";


            if (firstBar != null)
            {
                contentSymbol =
                    !string.IsNullOrWhiteSpace(
                        firstBar.PersianTicker)
                        ? firstBar.PersianTicker
                        : firstBar.EnglishTicker;
            }


            if (string.IsNullOrWhiteSpace(
                    contentSymbol))
            {
                contentSymbol =
                    Path.GetFileNameWithoutExtension(
                        filePath);
            }


            string displayName =
                contentSymbol;


            if (firstBar != null &&
                !string.IsNullOrWhiteSpace(
                    firstBar.PersianTicker))
            {
                displayName =
                    firstBar.PersianTicker;
            }


            var contentSymbolInfo =
                new SymbolInfo
                {
                    Symbol =
                        contentSymbol,

                    DisplayName =
                        displayName,

                    FilePath =
                        filePath,

                    IsSelected =
                        false
                };


            ApplyLastBarInfo(
                contentSymbolInfo,
                lastBar);


            return contentSymbolInfo;
        }


        // =========================================================
        // Single File
        // =========================================================

        private List<SymbolInfo> GetSymbolsFromFile(
            string filePath,
            DataSource dataSource,
            CancellationToken cancellationToken)
        {
            if (!File.Exists(filePath))
            {
                return new List<SymbolInfo>();
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                SymbolInfo? symbolInfo =
                    CreateSymbolInfoFromFile(
                        filePath,
                        dataSource,
                        cancellationToken);

                if (symbolInfo == null)
                {
                    return new List<SymbolInfo>();
                }

                symbolInfo.RowNumber =
                    1;

                return new List<SymbolInfo>
                {
                    symbolInfo
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return new List<SymbolInfo>();
            }
        }


        // =========================================================
        // Enrich
        // =========================================================

        private void EnrichSymbolInfoFromSummary(
            SymbolInfo symbolInfo,
            string filePath,
            DataSource dataSource,
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                (
                    MarketBar? FirstBar,
                    MarketBar? LastBar)
                    summary =
                        ParseSummary(
                            filePath,
                            dataSource,
                            cancellationToken);

                ApplyLastBarInfo(
                    symbolInfo,
                    summary.LastBar);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // اختیاری
            }
        }


        // =========================================================
        // Apply Last Bar
        // =========================================================

        private static void ApplyLastBarInfo(
            SymbolInfo symbolInfo,
            MarketBar? lastBar)
        {
            if (symbolInfo == null ||
                lastBar == null)
            {
                return;
            }

            symbolInfo.LastTradeDate =
                lastBar.Timestamp;

            symbolInfo.LastVolume =
                lastBar.Volume;

            symbolInfo.LastOpen =
                lastBar.Open;

            symbolInfo.LastHigh =
                lastBar.High;

            symbolInfo.LastLow =
                lastBar.Low;

            symbolInfo.LastClose =
                lastBar.Close;

            symbolInfo.LastFinalFee =
                lastBar.TSEClose;
        }


        // =========================================================
        // Load Bars
        // =========================================================

        public List<MarketBar> LoadBars(
            SymbolInfo symbolInfo,
            Portfolio portfolio)
        {
            if (symbolInfo == null)
            {
                throw new ArgumentNullException(
                    nameof(symbolInfo));
            }

            if (portfolio == null)
            {
                throw new ArgumentNullException(
                    nameof(portfolio));
            }

            if (portfolio.DataSource == null)
            {
                return new List<MarketBar>();
            }

            return ParseFile(
                symbolInfo.FilePath,
                portfolio.DataSource);
        }


        // =========================================================
        // Parse Full File
        // =========================================================

        private List<MarketBar> ParseFile(
            string filePath,
            DataSource dataSource)
        {
            if (!File.Exists(filePath))
            {
                return new List<MarketBar>();
            }

            var parser =
                new TseDailyParser();

            return parser.Parse(
                filePath,
                dataSource);
        }


        // =========================================================
        // Parse Summary
        // =========================================================

        private (
            MarketBar? FirstBar,
            MarketBar? LastBar)
            ParseSummary(
                string filePath,
                DataSource dataSource,
                CancellationToken cancellationToken)
        {
            if (!File.Exists(filePath))
            {
                return (
                    null,
                    null);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var parser =
                new TseDailyParser();

            return parser.ParseSummary(
                filePath,
                dataSource);
        }


        // =========================================================
        // Normalize Path
        // =========================================================

        private static string NormalizePath(
            string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "";
            }

            try
            {
                return Path.GetFullPath(path)
                    .TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return path.Trim();
            }
        }


        // =========================================================
        // Is Data File
        // =========================================================

        private static bool IsDataFile(
            string filePath)
        {
            string extension =
                Path.GetExtension(filePath);

            return
                string.Equals(
                    extension,
                    ".csv",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    extension,
                    ".txt",
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}