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
        public SymbolDataService() { }

        public List<SymbolInfo> GetSymbols(Portfolio portfolio) => GetSymbols(portfolio, CancellationToken.None);

        public List<SymbolInfo> GetSymbols(Portfolio portfolio, CancellationToken cancellationToken)
        {
            if (portfolio == null) throw new ArgumentNullException(nameof(portfolio));
            if (portfolio.DataSource == null) return new List<SymbolInfo>();
            string dataPath = portfolio.DataSource.Path ?? "";
            if (string.IsNullOrWhiteSpace(dataPath)) return new List<SymbolInfo>();

            List<SymbolInfo> result;
            if (portfolio.DataSource.SourceType == "Folder")
            {
                result = portfolio.UseExplicitSymbolList
                    ? GetExplicitSymbolsFromFolder(portfolio, dataPath, portfolio.DataSource, cancellationToken)
                    : GetSymbolsFromFolder(dataPath, portfolio.DataSource, cancellationToken);
            }
            else if (portfolio.DataSource.SourceType == "File")
            {
                cancellationToken.ThrowIfCancellationRequested();
                result = GetSymbolsFromFile(dataPath, portfolio.DataSource, cancellationToken);
            }
            else return new List<SymbolInfo>();

            for (int i = 0; i < result.Count; i++) { result[i].RowNumber = i + 1; result[i].IsSelected = false; }
            return result;
        }

        private List<SymbolInfo> GetExplicitSymbolsFromFolder(Portfolio portfolio, string folderPath, DataSource dataSource, CancellationToken cancellationToken)
        {
            if (!Directory.Exists(folderPath) || portfolio.Symbols == null || portfolio.Symbols.Count == 0) return new List<SymbolInfo>();
            List<string> paths = portfolio.Symbols.Where(x => x != null && !string.IsNullOrWhiteSpace(x.FilePath)).Select(x => NormalizePath(x.FilePath)).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (paths.Count == 0) return new List<SymbolInfo>();
            var parsed = new ConcurrentBag<SymbolInfo>();
            ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Math.Min(Environment.ProcessorCount, 4)), CancellationToken = cancellationToken };
            Parallel.ForEach(paths, options, filePath => { cancellationToken.ThrowIfCancellationRequested(); if (!File.Exists(filePath)) return; try { SymbolInfo? info = CreateSymbolInfoFromFile(filePath, dataSource, cancellationToken); if (info != null) parsed.Add(info); } catch (OperationCanceledException) { throw; } catch { } });
            return parsed.OrderBy(x => x.Symbol, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public Task<List<SymbolInfo>> GetSymbolsAsync(Portfolio portfolio, CancellationToken cancellationToken = default) => Task.Run(() => GetSymbols(portfolio, cancellationToken), cancellationToken);

        private List<SymbolInfo> GetSymbolsFromFolder(string folderPath, DataSource dataSource, CancellationToken cancellationToken)
        {
            if (!Directory.Exists(folderPath)) return new List<SymbolInfo>();
            cancellationToken.ThrowIfCancellationRequested();
            string[] files = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly).Where(IsDataFile).ToArray();
            if (files.Length == 0) return new List<SymbolInfo>();
            var parsed = new ConcurrentBag<SymbolInfo>();
            ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(2, Math.Min(Environment.ProcessorCount, 8)), CancellationToken = cancellationToken };
            Parallel.ForEach(files, options, file => { cancellationToken.ThrowIfCancellationRequested(); try { SymbolInfo? info = CreateSymbolInfoFromFile(file, dataSource, cancellationToken); if (info != null) parsed.Add(info); } catch (OperationCanceledException) { throw; } catch { } });
            return parsed.OrderBy(x => x.Symbol, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private SymbolInfo? CreateSymbolInfoFromFile(string filePath, DataSource dataSource, CancellationToken cancellationToken)
        {
            if (!File.Exists(filePath)) return null;
            cancellationToken.ThrowIfCancellationRequested();
            if (dataSource.SymbolSource == "FileName")
            {
                string fileSymbol = Path.GetFileNameWithoutExtension(filePath);
                var info = new SymbolInfo { Symbol = fileSymbol, DisplayName = fileSymbol, FilePath = filePath, IsSelected = false };
                EnrichSymbolInfoFromSummary(info, filePath, dataSource, cancellationToken);
                return info;
            }
            var summary = ParseSummary(filePath, dataSource, cancellationToken);
            if (summary.FirstBar == null && summary.LastBar == null) return null;
            MarketBar? firstBar = summary.FirstBar, lastBar = summary.LastBar;
            string contentSymbol = firstBar != null && !string.IsNullOrWhiteSpace(firstBar.PersianTicker) ? firstBar.PersianTicker : firstBar?.EnglishTicker ?? "";
            if (string.IsNullOrWhiteSpace(contentSymbol)) contentSymbol = Path.GetFileNameWithoutExtension(filePath);
            var contentInfo = new SymbolInfo { Symbol = contentSymbol, DisplayName = firstBar != null && !string.IsNullOrWhiteSpace(firstBar.PersianTicker) ? firstBar.PersianTicker : contentSymbol, FilePath = filePath, IsSelected = false };
            ApplyLastBarInfo(contentInfo, lastBar);
            return contentInfo;
        }

        private List<SymbolInfo> GetSymbolsFromFile(string filePath, DataSource dataSource, CancellationToken cancellationToken)
        {
            if (!File.Exists(filePath)) return new List<SymbolInfo>();
            try { cancellationToken.ThrowIfCancellationRequested(); SymbolInfo? info = CreateSymbolInfoFromFile(filePath, dataSource, cancellationToken); if (info == null) return new List<SymbolInfo>(); info.RowNumber = 1; return new List<SymbolInfo> { info }; }
            catch (OperationCanceledException) { throw; } catch { return new List<SymbolInfo>(); }
        }

        private void EnrichSymbolInfoFromSummary(SymbolInfo symbolInfo, string filePath, DataSource dataSource, CancellationToken cancellationToken)
        {
            try { cancellationToken.ThrowIfCancellationRequested(); var summary = ParseSummary(filePath, dataSource, cancellationToken); ApplyLastBarInfo(symbolInfo, summary.LastBar); } catch (OperationCanceledException) { throw; } catch { }
        }

        private static void ApplyLastBarInfo(SymbolInfo symbolInfo, MarketBar? lastBar)
        {
            if (symbolInfo == null || lastBar == null) return;
            symbolInfo.LastTradeDate = lastBar.Timestamp;
            symbolInfo.LastTradeDateText = lastBar.JalaliDate ?? "";
            symbolInfo.LastVolume = lastBar.Volume;
            symbolInfo.LastOpen = lastBar.Open;
            symbolInfo.LastHigh = lastBar.High;
            symbolInfo.LastLow = lastBar.Low;
            symbolInfo.LastClose = lastBar.Close;
            symbolInfo.LastFinalFee = lastBar.TSEClose;
        }

        public List<MarketBar> LoadBars(SymbolInfo symbolInfo, Portfolio portfolio)
        {
            if (symbolInfo == null) throw new ArgumentNullException(nameof(symbolInfo));
            if (portfolio == null) throw new ArgumentNullException(nameof(portfolio));
            if (portfolio.DataSource == null) return new List<MarketBar>();
            return ParseFile(symbolInfo.FilePath, portfolio.DataSource);
        }

        private List<MarketBar> ParseFile(string filePath, DataSource dataSource) => !File.Exists(filePath) ? new List<MarketBar>() : new TseDailyParser().Parse(filePath, dataSource);

        private (MarketBar? FirstBar, MarketBar? LastBar) ParseSummary(string filePath, DataSource dataSource, CancellationToken cancellationToken)
        {
            if (!File.Exists(filePath)) return (null, null);
            cancellationToken.ThrowIfCancellationRequested();
            return new TseDailyParser().ParseSummary(filePath, dataSource);
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "";
            try { return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); } catch { return path.Trim(); }
        }

        private static bool IsDataFile(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            return string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase);
        }
    }
}