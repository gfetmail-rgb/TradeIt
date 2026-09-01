using System;

namespace TradeIt.Models
{
    public class SymbolInfo
    {
        public string Symbol { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public int RowNumber { get; set; }
        public bool IsSelected { get; set; }
        public DateTime? LastTradeDate { get; set; }
        public string LastTradeDateText { get; set; } = "";
        public double LastVolume { get; set; }
        public double LastOpen { get; set; }
        public double LastHigh { get; set; }
        public double LastLow { get; set; }
        public double LastClose { get; set; }
        public double LastFinalFee { get; set; }
    }
}