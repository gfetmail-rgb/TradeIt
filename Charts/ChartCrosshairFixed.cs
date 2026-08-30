using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

using WpfPoint = System.Windows.Point;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private ScottPlot.Plottables.VerticalLine? _fixedCrosshairVertical;
        private ScottPlot.Plottables.HorizontalLine? _fixedCrosshairHorizontal;
        private ScottPlot.Plottables.Marker? _fixedCrosshairMarker;
        private bool _fixedCrosshairInitialized;

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            Loaded += ChartTabView_FixedCrosshairLoaded;
            Unloaded += ChartTabView_FixedCrosshairUnloaded;
        }

        private void ChartTabView_FixedCrosshairLoaded(object sender, RoutedEventArgs e)
        {
            if (_fixedCrosshairInitialized)
                return;

            _fixedCrosshairInitialized = true;

            // Use MouseMove (not PreviewMouseMove) so this runs after the
            // existing chart mouse handler and becomes the final crosshair state.
            Chart.MouseMove += FixedCrosshair_MouseMove;
            Chart.MouseLeave += FixedCrosshair_MouseLeave;
        }

        private void ChartTabView_FixedCrosshairUnloaded(object sender, RoutedEventArgs e)
        {
            if (!_fixedCrosshairInitialized)
                return;

            Chart.MouseMove -= FixedCrosshair_MouseMove;
            Chart.MouseLeave -= FixedCrosshair_MouseLeave;
            _fixedCrosshairInitialized = false;
        }

        private void FixedCrosshair_MouseMove(object sender, WpfMouseEventArgs e)
        {
            if (!_chartVisible || !_crosshairVisible || _bars.Count == 0)
                return;

            WpfPoint mouse = e.GetPosition(Chart);

            if (!TryGetChartCoordinates(
                    Chart,
                    mouse,
                    out ScottPlot.Coordinates coordinates))
            {
                return;
            }

            double x = GetNearestCandleX(coordinates.X);
            double y = coordinates.Y;

            EnsureFixedCrosshairPlottables(x, y);

            // Disable the old ScottPlot Crosshair rendering. The explicit
            // VerticalLine + HorizontalLine below are used because their
            // labels are rendered directly by the axis-line system.
            if (_crosshair != null)
            {
                _crosshair.IsVisible = false;
                _crosshair.VerticalLine.IsVisible = false;
                _crosshair.HorizontalLine.IsVisible = false;
                _crosshair.VerticalLine.Label.IsVisible = false;
                _crosshair.HorizontalLine.Label.IsVisible = false;
            }

            _fixedCrosshairVertical!.X = x;
            _fixedCrosshairHorizontal!.Y = y;
            _fixedCrosshairMarker!.X = x;
            _fixedCrosshairMarker!.Y = y;

            string xText = FormatCrosshairX(x);
            string yText = y.ToString("N2");

            _fixedCrosshairVertical.Text = xText;
            _fixedCrosshairHorizontal.Text = yText;

            _fixedCrosshairVertical.IsVisible = true;
            _fixedCrosshairHorizontal.IsVisible = true;
            _fixedCrosshairMarker.IsVisible = true;

            UpdateMouseInformation(
                new ScottPlot.Coordinates(x, y));

            Chart.Refresh();
        }

        private void EnsureFixedCrosshairPlottables(double x, double y)
        {
            bool verticalExists =
                _fixedCrosshairVertical != null &&
                Chart.Plot.GetPlottables().Contains(_fixedCrosshairVertical);

            bool horizontalExists =
                _fixedCrosshairHorizontal != null &&
                Chart.Plot.GetPlottables().Contains(_fixedCrosshairHorizontal);

            bool markerExists =
                _fixedCrosshairMarker != null &&
                Chart.Plot.GetPlottables().Contains(_fixedCrosshairMarker);

            if (!verticalExists)
            {
                _fixedCrosshairVertical =
                    Chart.Plot.Add.VerticalLine(x);

                _fixedCrosshairVertical.Color =
                    ScottPlot.Color.FromHtml("#707070");
                _fixedCrosshairVertical.LineWidth = 1;
                _fixedCrosshairVertical.LinePattern =
                    ScottPlot.LinePattern.Dashed;
                _fixedCrosshairVertical.ExcludeFromLegend = true;

                _fixedCrosshairVertical.LabelOppositeAxis = false;
                _fixedCrosshairVertical.LabelRotation = 0;
                _fixedCrosshairVertical.LabelAlignment =
                    ScottPlot.Alignment.LowerCenter;
                _fixedCrosshairVertical.LabelBackgroundColor =
                    ScottPlot.Color.FromHtml("#202020");
                _fixedCrosshairVertical.LabelFontColor =
                    ScottPlot.Color.FromHtml("#FFFFFF");
                _fixedCrosshairVertical.LabelFontSize = 12;
                _fixedCrosshairVertical.LabelBold = true;
                _fixedCrosshairVertical.LabelPadding = 4;
            }

            if (!horizontalExists)
            {
                _fixedCrosshairHorizontal =
                    Chart.Plot.Add.HorizontalLine(y);

                _fixedCrosshairHorizontal.Color =
                    ScottPlot.Color.FromHtml("#707070");
                _fixedCrosshairHorizontal.LineWidth = 1;
                _fixedCrosshairHorizontal.LinePattern =
                    ScottPlot.LinePattern.Dashed;
                _fixedCrosshairHorizontal.ExcludeFromLegend = true;

                _fixedCrosshairHorizontal.LabelOppositeAxis = false;
                _fixedCrosshairHorizontal.LabelRotation = 0;
                _fixedCrosshairHorizontal.LabelAlignment =
                    ScottPlot.Alignment.MiddleRight;
                _fixedCrosshairHorizontal.LabelBackgroundColor =
                    ScottPlot.Color.FromHtml("#202020");
                _fixedCrosshairHorizontal.LabelFontColor =
                    ScottPlot.Color.FromHtml("#FFFFFF");
                _fixedCrosshairHorizontal.LabelFontSize = 12;
                _fixedCrosshairHorizontal.LabelBold = true;
                _fixedCrosshairHorizontal.LabelPadding = 4;
            }

            if (!markerExists)
            {
                _fixedCrosshairMarker =
                    Chart.Plot.Add.Marker(
                        x,
                        y,
                        ScottPlot.MarkerShape.Cross);

                _fixedCrosshairMarker.MarkerSize = 10;
                _fixedCrosshairMarker.LineWidth = 2;
                _fixedCrosshairMarker.MarkerLineColor =
                    ScottPlot.Color.FromHtml("#202020");
                _fixedCrosshairMarker.MarkerFillColor =
                    ScottPlot.Color.FromHtml("#FFFFFF");
                _fixedCrosshairMarker.ExcludeFromLegend = true;
            }
        }

        private void FixedCrosshair_MouseLeave(
            object sender,
            WpfMouseEventArgs e)
        {
            if (_fixedCrosshairVertical != null)
                _fixedCrosshairVertical.IsVisible = false;

            if (_fixedCrosshairHorizontal != null)
                _fixedCrosshairHorizontal.IsVisible = false;

            if (_fixedCrosshairMarker != null)
                _fixedCrosshairMarker.IsVisible = false;
        }
    }
}
