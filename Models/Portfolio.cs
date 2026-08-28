using System.Collections.Generic;

namespace TradeIt.Models
{
    public class Portfolio
    {
        public string Name { get; set; } = "";

        public DataSource DataSource { get; set; } = new();

        public List<SymbolInfo> Symbols { get; set; } = new();

        // =========================================================
        // مشخص می‌کند که لیست Symbols یک لیست صریح و محدودکننده است.
        //
        // false:
        // همه فایل‌های DataSource در سبد هستند.
        //
        // true:
        // فقط SymbolInfo های داخل Symbols متعلق به سبد هستند.
        // =========================================================

        public bool UseExplicitSymbolList { get; set; } = false;
    }
}