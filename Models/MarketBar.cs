using System;

namespace TradeIt.Models
{
    public class MarketBar
    {
        public int Index { get; set; }

        public string PersianTicker { get; set; } = "";

        public string EnglishTicker { get; set; } = "";

        // Original date text exactly as it appeared in the source file.
        public string JalaliDate { get; set; } = "";

        // Source calendar: Persian or Gregorian. Empty means no date is available.
        public string Calendar { get; set; } = "";

        public string Time { get; set; } = "";

        public double Open { get; set; }

        private double _high;
        public double High
        {
            get => Math.Max(_high, Math.Max(Open, Close));
            set => _high = value;
        }

        private double _low;
        public double Low
        {
            get => Math.Min(_low, Math.Min(Open, Close));
            set => _low = value;
        }

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