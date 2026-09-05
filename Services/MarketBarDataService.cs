using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TradeIt.Data;
using TradeIt.Models;
using TradeIt.Portfolios;

namespace TradeIt.Services
{
    /// <summary>
    /// Loads the complete OHLCV series for a selected symbol.
    /// This is deliberately separate from symbol-universe discovery.
    /// </summary>
    internal sealed class MarketBarDataService
    {
        public List<MarketBar> LoadBars(SymbolInfo symbolInfo, Portfolio portfolio)
        {
            if (symbolInfo == null) throw new ArgumentNullException(nameof(symbolInfo));
            if (portfolio == null) throw new ArgumentNullException(nameof(portfolio));
            if (portfolio.DataSource == null) return new List<MarketBar>();

            List<MarketBar> bars = ParseFile(symbolInfo.FilePath, portfolio.DataSource);
            if (portfolio.DataSource.SourceType == "File" &&
                string.Equals(portfolio.DataSource.SymbolSource, "FileContent", StringComparison.OrdinalIgnoreCase))
            {
                bars = bars
                    .Where(x => string.Equals(
                        x.PersianTicker,
                        symbolInfo.Symbol,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();

                for (int i = 0; i < bars.Count; i++)
                    bars[i].Index = i;
            }

            return bars;
        }

        private static List<MarketBar> ParseFile(string filePath, DataSource dataSource) =>
            !File.Exists(filePath)
                ? new List<MarketBar>()
                : new TseDailyParser().Parse(filePath, dataSource);
    }
}
