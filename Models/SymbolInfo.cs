using System;

namespace TradeIt.Models
{
    public class SymbolInfo
    {
        // =========================================================
        // Basic
        // =========================================================

        public string Symbol { get; set; } = "";

        public string DisplayName { get; set; } = "";

        public string FilePath { get; set; } = "";

        // =========================================================
        // User classification
        // =========================================================

        public string SecurityType { get; set; } = "";

        public string Industry { get; set; } = "";

        public string Group { get; set; } = "";

        public string SubGroup { get; set; } = "";

        // =========================================================
        // UI
        // =========================================================

        public int RowNumber { get; set; }

        public bool IsSelected { get; set; }

        // =========================================================
        // Last Trade Information
        // =========================================================

        public DateTime? LastTradeDate { get; set; }

        public double LastVolume { get; set; }

        public double LastOpen { get; set; }

        public double LastHigh { get; set; }

        public double LastLow { get; set; }

        public double LastClose { get; set; }

        // قیمت پایانی بورس ایران
        public double LastFinalFee { get; set; }
    }
}
