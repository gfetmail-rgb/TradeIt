using System.Collections.Generic;

namespace TradeIt.Charts
{
    public sealed class DrawingToolStyle
    {
        public string Color { get; set; } = "#1976D2";
        public string BackgroundColor { get; set; } = "#FFFFFF";
        public double LineWidth { get; set; } = 1.5;
        public string LineStyle { get; set; } = "Solid";
        public string FontFamily { get; set; } = "Segoe UI";
        public double FontSize { get; set; } = 14;
        public Dictionary<string, bool> FibonacciLevels { get; set; } = new();

        public DrawingToolStyle Clone() => new()
        {
            Color = Color,
            BackgroundColor = BackgroundColor,
            LineWidth = LineWidth,
            LineStyle = LineStyle,
            FontFamily = FontFamily,
            FontSize = FontSize,
            FibonacciLevels = FibonacciLevels == null ? new Dictionary<string, bool>() : new Dictionary<string, bool>(FibonacciLevels)
        };
    }

    public class ChartSettings
    {
        public string RisingColor { get; set; } = "#00A86B";
        public string FallingColor { get; set; } = "#E74C3C";
        public string LineColor { get; set; } = "#1976D2";
        public string VolumeColor { get; set; } = "#607D8B";
        public double VolumeBarWidth { get; set; } = 0.8;
        public string FigureBackground { get; set; } = "#FFFFFF";
        public string DataBackground { get; set; } = "#FFFFFF";
        public string GridColor { get; set; } = "#DDDDDD";
        public string GridPattern { get; set; } = "Solid";
        public double GridLineWidth { get; set; } = 1;
        public string AxisColor { get; set; } = "#444444";
        public double LineWidth { get; set; } = 1.5;
        public double CandleLineWidth { get; set; } = 1;
        public double BarLineWidth { get; set; } = 1;
        public bool GridVisible { get; set; } = false;
        public bool CrosshairVisible { get; set; } = true;
        public bool VolumeVisible { get; set; } = false;
        public int AutoScrollDelayMilliseconds { get; set; } = 1000;
        public bool OpenChartInNewTab { get; set; } = true;
        public string CrosshairColor { get; set; } = "#909090";
        public double CrosshairLineWidth { get; set; } = 1;
        public string CrosshairPattern { get; set; } = "Dotted";
        public string ChartType { get; set; } = "Candlestick";
        public bool ShowTimeGaps { get; set; } = true;
        public bool HasUserSavedSettings { get; set; } = false;
        public Dictionary<string, DrawingToolStyle> DrawingToolStyles { get; set; } = CreateDefaultDrawingToolStyles();

        public static Dictionary<string, bool> CreateDefaultRetracementLevels() => new()
        {
            ["0.0"] = true, ["23.6"] = true, ["38.2"] = true, ["50.0"] = true,
            ["61.8"] = true, ["78.6"] = true, ["100.0"] = true
        };

        public static Dictionary<string, bool> CreateDefaultExtensionLevels() => new()
        {
            ["0.0"] = true, ["38.2"] = true, ["61.8"] = true, ["100.0"] = true,
            ["127.2"] = true, ["161.8"] = true, ["261.8"] = true
        };

        public static Dictionary<string, DrawingToolStyle> CreateDefaultDrawingToolStyles() => new()
        {
            ["TrendLine"] = new DrawingToolStyle { Color = "#1976D2", LineWidth = 1.5, LineStyle = "Solid" },
            ["Arrow"] = new DrawingToolStyle { Color = "#1976D2", LineWidth = 1.5, LineStyle = "Solid" },
            ["HorizontalLine"] = new DrawingToolStyle { Color = "#1976D2", LineWidth = 1.5, LineStyle = "Solid" },
            ["VerticalLine"] = new DrawingToolStyle { Color = "#1976D2", LineWidth = 1.5, LineStyle = "Solid" },
            ["HorizontalRay"] = new DrawingToolStyle { Color = "#8E44AD", LineWidth = 1.5, LineStyle = "Solid" },
            ["ParallelChannel"] = new DrawingToolStyle { Color = "#D35400", LineWidth = 1.5, LineStyle = "Solid" },
            ["Rectangle"] = new DrawingToolStyle { Color = "#16A085", LineWidth = 1.5, LineStyle = "Solid" },
            ["Pitchfork"] = new DrawingToolStyle { Color = "#C0392B", LineWidth = 1.5, LineStyle = "Dash" },
            ["FibonacciRetracement"] = new DrawingToolStyle { Color = "#8E44AD", LineWidth = 1.25, LineStyle = "Dash", FibonacciLevels = CreateDefaultRetracementLevels() },
            ["FibonacciExtension"] = new DrawingToolStyle { Color = "#2C3E50", LineWidth = 1.25, LineStyle = "Dash", FibonacciLevels = CreateDefaultExtensionLevels() },
            ["Text"] = new DrawingToolStyle { Color = "#1976D2", BackgroundColor = "#FFFFFF", FontFamily = "Segoe UI", FontSize = 14 }
        };

        public ChartSettings Clone()
        {
            var clone = new ChartSettings
            {
                RisingColor = RisingColor, FallingColor = FallingColor, LineColor = LineColor,
                VolumeColor = VolumeColor, VolumeBarWidth = VolumeBarWidth, FigureBackground = FigureBackground,
                DataBackground = DataBackground, GridColor = GridColor, GridPattern = GridPattern,
                GridLineWidth = GridLineWidth, AxisColor = AxisColor, LineWidth = LineWidth,
                CandleLineWidth = CandleLineWidth, BarLineWidth = BarLineWidth, GridVisible = GridVisible,
                CrosshairVisible = CrosshairVisible, VolumeVisible = VolumeVisible,
                AutoScrollDelayMilliseconds = AutoScrollDelayMilliseconds, OpenChartInNewTab = OpenChartInNewTab,
                CrosshairColor = CrosshairColor, CrosshairLineWidth = CrosshairLineWidth,
                CrosshairPattern = CrosshairPattern, ChartType = ChartType, ShowTimeGaps = ShowTimeGaps,
                HasUserSavedSettings = HasUserSavedSettings, DrawingToolStyles = new Dictionary<string, DrawingToolStyle>()
            };

            foreach (var pair in DrawingToolStyles)
                clone.DrawingToolStyles[pair.Key] = pair.Value.Clone();
            foreach (var pair in CreateDefaultDrawingToolStyles())
                if (!clone.DrawingToolStyles.ContainsKey(pair.Key))
                    clone.DrawingToolStyles[pair.Key] = pair.Value;
            return clone;
        }
    }
}
