
using System;

namespace TradeIt.Models
{
    public class SymbolMarketInfo
    {
        // =========================================================
        // Last Trading Day
        // =========================================================

        public DateTime? LastTradeDate { get; set; }


        // =========================================================
        // Previous Trading Day
        // =========================================================

        public DateTime? PreviousTradeDate { get; set; }


        // =========================================================
        // Last Day - OHLC
        // =========================================================

        public double LastOpen { get; set; }

        public double LastHigh { get; set; }

        public double LastLow { get; set; }

        public double LastClose { get; set; }


        // =========================================================
        // FINAL FEE
        //
        // قیمت پایانی بورس ایران
        // =========================================================

        public double LastFinalFee { get; set; }


        public double LastTSEClose { get; set; }


        // =========================================================
        // Last Volume
        // =========================================================

        public double LastVolume { get; set; }


        // =========================================================
        // Previous Day - OHLC
        // =========================================================

        public double PreviousOpen { get; set; }

        public double PreviousHigh { get; set; }

        public double PreviousLow { get; set; }

        public double PreviousClose { get; set; }


        // =========================================================
        // Previous FINAL FEE
        // =========================================================

        public double PreviousFinalFee { get; set; }


        public double PreviousTSEClose { get; set; }


        // =========================================================
        // Volume Average
        // =========================================================

        public double AverageVolume { get; set; }

        public int VolumeAverageDays { get; set; }


        // =========================================================
        // Days Since Last Trade
        // =========================================================

        public int DaysSinceLastTrade { get; set; }
    }
}

