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
        public string GridPattern { get; set; } = "Solid";
        public double GridLineWidth { get; set; } = 1;
        public string AxisColor { get; set; } = "#444444";
        public double LineWidth { get; set; } = 2;
        public bool GridVisible { get; set; } = false;
        public int AutoScrollDelayMilliseconds { get; set; } = 1000;
        public bool OpenChartInNewTab { get; set; } = true;

        public string CrosshairColor { get; set; } = "#909090";
        public double CrosshairLineWidth { get; set; } = 1;
        public string CrosshairPattern { get; set; } = "Dotted";

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
                GridPattern = GridPattern,
                GridLineWidth = GridLineWidth,
                AxisColor = AxisColor,
                LineWidth = LineWidth,
                GridVisible = GridVisible,
                AutoScrollDelayMilliseconds = AutoScrollDelayMilliseconds,
                OpenChartInNewTab = OpenChartInNewTab,
                CrosshairColor = CrosshairColor,
                CrosshairLineWidth = CrosshairLineWidth,
                CrosshairPattern = CrosshairPattern
            };
        }
    }
}