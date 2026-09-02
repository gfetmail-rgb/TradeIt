namespace TradeIt.Models
{
    public enum TradeStatusFilter { All, TradedToday, NotTradedToday }
    public enum SymbolNameFilter { All, Contains, StartsWith, EndsWith, Middle, DoesNotContain }
    public enum NumericComparison { GreaterThan, GreaterOrEqual, Equal, LessOrEqual, LessThan, NotEqual }
    public enum PriceField { Open, High, Low, Close, Volume, FinalFee }

    public class PriceFilter
    {
        public bool Enabled { get; set; }

        // هر فیلتر یک مقایسه کامل بین دو مقدار تاریخی است.
        // DayOffset = 0 یعنی کندل جاری، 1 یعنی یک کندل قبل، و ...
        public PriceField LeftField { get; set; }
        public int LeftDayOffset { get; set; }
        public NumericComparison Comparison { get; set; }
        public PriceField RightField { get; set; }
        public int RightDayOffset { get; set; }
    }

    public class SymbolFilterSettings
    {
        public TradeStatusFilter TradeStatus { get; set; } = TradeStatusFilter.All;
        public SymbolNameFilter NameFilter { get; set; } = SymbolNameFilter.All;
        public string NameText { get; set; } = "";
        public bool DaysWithoutTradeEnabled { get; set; }
        public int DaysWithoutTrade { get; set; }
        public bool DaysWithTradeEnabled { get; set; }
        public int DaysWithTrade { get; set; }
        public bool VolumeFilterEnabled { get; set; }
        public int VolumeAverageDays { get; set; } = 20;
        public double VolumeMultiplier { get; set; } = 2.0;
        public PriceFilter[] PriceFilters { get; set; } = new PriceFilter[5]
        {
            new PriceFilter(), new PriceFilter(), new PriceFilter(), new PriceFilter(), new PriceFilter()
        };
    }
}