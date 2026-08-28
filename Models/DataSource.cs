namespace TradeIt.Models
{
    public class DataSource
    {
        // Folder یا File
        public string SourceType { get; set; } = "Folder";

        // مسیر فایل یا پوشه
        public string Path { get; set; } = "";

        // جداکننده
        public string Delimiter { get; set; } = ",";

        // آیا ردیف اول Header است؟
        public bool HasHeader { get; set; } = true;

        // =========================================================
        // منبع نام نماد
        // FileName    = نام فایل
        // FileContent = داخل فایل
        // =========================================================

        public string SymbolSource { get; set; } = "FileName";

        // =========================================================
        // نوع داده
        // =========================================================

        public string DataType { get; set; } = "TseDaily";

        // =========================================================
        // تاریخ و زمان
        // =========================================================

        public bool HasDateTime { get; set; } = true;

        // Persian یا Gregorian
        public string Calendar { get; set; } = "Persian";

        public string DateFormat { get; set; } = "yyyyMMdd";

        public string TimeFormat { get; set; } = "HHmmss";

        // =========================================================
        // Mapping
        // =========================================================

        public int SymbolColumn { get; set; } = -1;

        public int DateColumn { get; set; } = -1;

        public int TimeColumn { get; set; } = -1;

        public int OpenColumn { get; set; } = -1;

        public int HighColumn { get; set; } = -1;

        public int LowColumn { get; set; } = -1;

        public int CloseColumn { get; set; } = -1;

        public int VolumeColumn { get; set; } = -1;

        public int TSECloseColumn { get; set; } = -1;

        public int PreviousColumn { get; set; } = -1;

        public int ValueColumn { get; set; } = -1;

        public int TradeCountColumn { get; set; } = -1;

        public int EnglishTickerColumn { get; set; } = -1;

        public int ShareCountColumn { get; set; } = -1;

        public int MarketValueColumn { get; set; } = -1;
    }
}