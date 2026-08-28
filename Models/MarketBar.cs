namespace TradeIt.Models
{
    public class MarketBar
    {
        public int Index { get; set; }

        public string PersianTicker { get; set; } = "";

        public string EnglishTicker { get; set; } = "";

        public string JalaliDate { get; set; } = "";

        public string Time { get; set; } = "";

        public double Open { get; set; }

        public double High { get; set; }

        public double Low { get; set; }

        public double Close { get; set; }

        public double Volume { get; set; }

        public double TSEClose { get; set; }

        public double Previous { get; set; }

        public double Value { get; set; }

        public int TradeCount { get; set; }

        public double ShareCount { get; set; }

        public double MarketValue { get; set; }

        public DateTime? Timestamp { get; set; }
    }
}