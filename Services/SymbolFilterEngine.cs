using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradeIt.Models;

namespace TradeIt.Services
{
    /// <summary>
    /// Business logic for symbol filtering. UI code should only capture settings
    /// and present the resulting symbols; historical filtering is kept here.
    /// </summary>
    internal sealed class SymbolFilterEngine
    {
        private readonly Dictionary<string, List<MarketBar>> _barsCache =
            new(StringComparer.OrdinalIgnoreCase);

        public async Task<List<SymbolInfo>> ApplyAsync(
            IEnumerable<SymbolInfo> symbols,
            string searchText,
            SymbolFilterSettings settings,
            CancellationToken cancellationToken,
            Func<SymbolInfo, List<MarketBar>> loadBars)
        {
            ArgumentNullException.ThrowIfNull(symbols);
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(loadBars);

            IEnumerable<SymbolInfo> query = symbols;
            string search = searchText?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.Symbol.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            DateTime? marketDate = symbols
                .Where(x => x.LastTradeDate.HasValue)
                .Select(x => x.LastTradeDate!.Value.Date)
                .OrderByDescending(x => x)
                .Cast<DateTime?>()
                .FirstOrDefault();

            if (settings.TradeStatus != TradeStatusFilter.All && marketDate.HasValue)
            {
                query = query.Where(x =>
                    (settings.TradeStatus == TradeStatusFilter.TradedToday) ==
                    (x.LastTradeDate.HasValue && x.LastTradeDate.Value.Date == marketDate.Value));
            }

            if (settings.NameFilter != SymbolNameFilter.All && !string.IsNullOrEmpty(settings.NameText))
            {
                query = query.Where(x => MatchName(x.DisplayName, settings.NameText, settings.NameFilter));
            }

            if (settings.DaysWithoutTradeEnabled && settings.DaysWithoutTrade > 0 && marketDate.HasValue)
            {
                DateTime cutoff = marketDate.Value.AddDays(-settings.DaysWithoutTrade);
                query = query.Where(x => !x.LastTradeDate.HasValue || x.LastTradeDate.Value.Date < cutoff);
            }

            if (settings.DaysWithTradeEnabled && settings.DaysWithTrade > 0 && marketDate.HasValue)
            {
                DateTime cutoff = marketDate.Value.AddDays(-settings.DaysWithTrade);
                query = query.Where(x => x.LastTradeDate.HasValue && x.LastTradeDate.Value.Date >= cutoff);
            }

            List<SymbolInfo> candidates = query.ToList();
            bool needsBars = settings.VolumeFilterEnabled || settings.PriceFilters.Any(x => x.Enabled);

            if (needsBars)
            {
                await EnsureBarsLoadedAsync(candidates, cancellationToken, loadBars);
                candidates = candidates.Where(x => PassHistoricalFilters(x, settings)).ToList();
            }

            cancellationToken.ThrowIfCancellationRequested();
            Renumber(candidates);
            return candidates;
        }

        public void ClearCache() => _barsCache.Clear();

        private async Task EnsureBarsLoadedAsync(
            List<SymbolInfo> symbols,
            CancellationToken cancellationToken,
            Func<SymbolInfo, List<MarketBar>> loadBars)
        {
            List<SymbolInfo> missing = symbols
                .Where(x => !_barsCache.ContainsKey(x.FilePath))
                .ToList();

            if (missing.Count == 0)
                return;

            await Task.Run(() =>
            {
                foreach (SymbolInfo symbol in missing)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    List<MarketBar> bars = loadBars(symbol)
                        .OrderBy(x => x.Timestamp ?? DateTime.MinValue)
                        .ToList();
                    lock (_barsCache)
                        _barsCache[symbol.FilePath] = bars;
                }
            }, cancellationToken);
        }

        private bool PassHistoricalFilters(SymbolInfo symbol, SymbolFilterSettings settings)
        {
            if (!_barsCache.TryGetValue(symbol.FilePath, out List<MarketBar>? bars) || bars.Count == 0)
                return false;

            if (settings.VolumeFilterEnabled)
            {
                int n = Math.Max(1, settings.VolumeAverageDays);
                if (bars.Count < n)
                    return false;

                double average = bars.TakeLast(n).Average(x => x.Volume);
                if (bars[^1].Volume < average * settings.VolumeMultiplier)
                    return false;
            }

            foreach (PriceFilter filter in settings.PriceFilters)
            {
                if (!filter.Enabled)
                    continue;

                int leftOffset = Math.Max(0, filter.LeftDayOffset);
                int rightOffset = Math.Max(0, filter.RightDayOffset);
                if (bars.Count <= leftOffset || bars.Count <= rightOffset)
                    return false;

                MarketBar leftBar = bars[bars.Count - 1 - leftOffset];
                MarketBar rightBar = bars[bars.Count - 1 - rightOffset];

                double leftValue = GetPriceField(leftBar, filter.LeftField);
                double rightValue = GetPriceField(rightBar, filter.RightField);

                if (!Compare(leftValue, rightValue, filter.Comparison))
                    return false;
            }

            return true;
        }

        private static bool MatchName(string value, string text, SymbolNameFilter filter)
        {
            if (filter == SymbolNameFilter.All)
                return true;
            if (filter == SymbolNameFilter.Contains)
                return value.Contains(text, StringComparison.OrdinalIgnoreCase);
            if (filter == SymbolNameFilter.StartsWith)
                return value.StartsWith(text, StringComparison.OrdinalIgnoreCase);
            if (filter == SymbolNameFilter.EndsWith)
                return value.EndsWith(text, StringComparison.OrdinalIgnoreCase);
            if (filter == SymbolNameFilter.DoesNotContain)
                return !value.Contains(text, StringComparison.OrdinalIgnoreCase);

            string[] terms = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (terms.Length == 0)
                return true;
            if (filter == SymbolNameFilter.ContainsAny)
                return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
            if (filter == SymbolNameFilter.DoesNotContainAny)
                return terms.All(term => !value.Contains(term, StringComparison.OrdinalIgnoreCase));

            int index = value.IndexOf(text, StringComparison.OrdinalIgnoreCase);
            return index > 0 && index + text.Length < value.Length;
        }

        private static double GetPriceField(MarketBar bar, PriceField field) => field switch
        {
            PriceField.Open => bar.Open,
            PriceField.High => bar.High,
            PriceField.Low => bar.Low,
            PriceField.Close => bar.Close,
            PriceField.Volume => bar.Volume,
            PriceField.FinalFee => bar.TSEClose,
            _ => double.NaN
        };

        private static bool Compare(double left, double right, NumericComparison comparison)
        {
            if (double.IsNaN(left) || double.IsNaN(right))
                return false;

            const double epsilon = 1e-9;
            return comparison switch
            {
                NumericComparison.GreaterThan => left > right,
                NumericComparison.GreaterOrEqual => left >= right,
                NumericComparison.Equal => Math.Abs(left - right) <= epsilon,
                NumericComparison.NotEqual => Math.Abs(left - right) > epsilon,
                NumericComparison.LessOrEqual => left <= right,
                NumericComparison.LessThan => left < right,
                _ => true
            };
        }

        private static void Renumber(List<SymbolInfo> symbols)
        {
            for (int i = 0; i < symbols.Count; i++)
                symbols[i].RowNumber = i + 1;
        }
    }
}
