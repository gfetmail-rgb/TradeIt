namespace TradeIt.Models
{
    public enum TradeStatusFilter
    {
        All,
        TradedToday,
        NotTradedToday
    }

    public enum SymbolNameFilter
    {
        All,
        Contains,
        StartsWith,
        EndsWith,
        Middle,
        DoesNotContain
    }

    public enum NumericComparison
    {
        GreaterThan,
        GreaterOrEqual,
        Equal,
        LessOrEqual,
        LessThan,
        NotEqual
    }

    public enum PriceField
    {
        Open,
        High,
        Low,
        Close,
        Volume,
        FinalFee
    }

    public class PriceFilter
    {
        public bool Enabled { get; set; }

        public PriceField Field { get; set; }

        public NumericComparison Comparison { get; set; }
    }

    public class SymbolFilterSettings
    {
        // =========================================================
        // معامله
        // =========================================================

        public TradeStatusFilter TradeStatus { get; set; }
            = TradeStatusFilter.All;

        // =========================================================
        // نام
        // =========================================================

        public SymbolNameFilter NameFilter { get; set; }
            = SymbolNameFilter.All;

        public string NameText { get; set; } = "";

        // =========================================================
        // عدم معامله
        // =========================================================

        public bool DaysWithoutTradeEnabled { get; set; }

        public int DaysWithoutTrade { get; set; }

        // =========================================================
        // Volume
        //
        // LastVolume >= Average(X days) * Y
        // =========================================================

        public bool VolumeFilterEnabled { get; set; }

        public int VolumeAverageDays { get; set; } = 20;

        public double VolumeMultiplier { get; set; } = 2.0;

        // =========================================================
        // پنج فیلتر قیمت
        //
        // همه با AND اعمال می‌شوند.
        // =========================================================

        public PriceFilter[] PriceFilters { get; set; }
            = new PriceFilter[5]
            {
                new PriceFilter(),
                new PriceFilter(),
                new PriceFilter(),
                new PriceFilter(),
                new PriceFilter()
            };
    }
}