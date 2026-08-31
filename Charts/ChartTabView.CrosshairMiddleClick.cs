using System.Windows.Input;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        protected override void OnInitialized(System.EventArgs e)
        {
            base.OnInitialized(e);

            Chart.PreviewMouseDown += Chart_PreviewMouseDownForCrosshair;
            VolumeChart.PreviewMouseDown += Chart_PreviewMouseDownForCrosshair;
        }

        private void Chart_PreviewMouseDownForCrosshair(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Middle)
                return;

            if (_crosshair == null || !_crosshairVisible)
                return;

            _crosshairVisible = false;
            _crosshairMouseInside = false;
            _crosshair.IsVisible = false;

            ChartInfoTextBlock.Text =
                $"{_symbol.Symbol} | Crosshair خاموش";

            Chart.Refresh();
            VolumeChart.Refresh();

            e.Handled = true;
        }
    }
}
