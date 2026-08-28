namespace TradeIt.Charts
{
    public class ChartSettings
    {
        public string RisingColor { get; set; } = "#00A86B";

        public string FallingColor { get; set; } = "#E74C3C";

        public string LineColor { get; set; } = "#1976D2";

        public string FigureBackground { get; set; } = "#FFFFFF";

        public string DataBackground { get; set; } = "#FFFFFF";

        public string GridColor { get; set; } = "#DDDDDD";

        public string AxisColor { get; set; } = "#444444";

        public double LineWidth { get; set; } = 2;

        public bool GridVisible { get; set; } = true;

        // =========================================================
        // Auto Scroll
        // =========================================================

        public int AutoScrollDelayMilliseconds { get; set; } = 1000;

        // =========================================================
        // Clone
        // =========================================================

        public ChartSettings Clone()
        {
            return new ChartSettings
            {
                RisingColor = RisingColor,
                FallingColor = FallingColor,
                LineColor = LineColor,
                FigureBackground = FigureBackground,
                DataBackground = DataBackground,
                GridColor = GridColor,
                AxisColor = AxisColor,
                LineWidth = LineWidth,
                GridVisible = GridVisible,

                AutoScrollDelayMilliseconds =
                    AutoScrollDelayMilliseconds
            };
        }
    }
}