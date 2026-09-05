using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TradeIt.Models;
using TradeIt.Portfolios;

namespace TradeIt.Services
{
    /// <summary>
    /// Facade for symbol data access.
    /// Symbol-universe discovery and OHLCV loading are implemented by dedicated services.
    /// </summary>
    public class SymbolDataService
    {
        private readonly SymbolUniverseService _symbolUniverseService;
        private readonly MarketBarDataService _marketBarDataService;

        public SymbolDataService()
        {
            _symbolUniverseService = new SymbolUniverseService();
            _marketBarDataService = new MarketBarDataService();
        }

        public List<SymbolInfo> GetSymbols(Portfolio portfolio) =>
            _symbolUniverseService.GetSymbols(portfolio);

        public List<SymbolInfo> GetSymbols(
            Portfolio portfolio,
            CancellationToken cancellationToken) =>
            _symbolUniverseService.GetSymbols(portfolio, cancellationToken);

        public Task<List<SymbolInfo>> GetSymbolsAsync(
            Portfolio portfolio,
            CancellationToken cancellationToken = default) =>
            _symbolUniverseService.GetSymbolsAsync(portfolio, cancellationToken);

        public List<MarketBar> LoadBars(
            SymbolInfo symbolInfo,
            Portfolio portfolio) =>
            _marketBarDataService.LoadBars(symbolInfo, portfolio);
    }
}
