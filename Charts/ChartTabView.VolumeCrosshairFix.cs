using System;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private void ApplyVolumeCrosshairSettings()
        {
            if (_volumeCrosshair == null)
                return;

            _volumeCrosshair.LineColor =
                ScottPlot.Color.FromHex(_settings?.CrosshairColor ?? "#909090");
            _volumeCrosshair.LineWidth =
                (float)Math.Max(0.1, _settings?.CrosshairLineWidth ?? 1.0);
            _volumeCrosshair.LinePattern =
                ParseVolumeCrosshairPattern(_settings?.CrosshairPattern);

            // Volume crosshair is vertical only. It deliberately has no horizontal line.
            _volumeCrosshair.HorizontalLine.IsVisible = false;
            _volumeCrosshair.VerticalLine.IsVisible = true;

            // Keep the volume crosshair visually minimal; no marker/price label is needed.
            _volumeCrosshair.MarkerSize = 0;
        }

        private static ScottPlot.LinePattern ParseVolumeCrosshairPattern(string? value)
        {
            return value?.Trim().ToLowerInvariant() switch
            {
                "dotted" => ScottPlot.LinePattern.Dotted,
                "dashed" => ScottPlot.LinePattern.Dashed,
                "denselydashed" => ScottPlot.LinePattern.DenselyDashed,
                _ => ScottPlot.LinePattern.Solid
            };
        }
    }
}
