namespace TradeIt.Charts
{
    // Settings synchronization is implemented centrally in ChartTabView.SettingsSync.cs.
    // This partial file also exposes a safe factory used by MainWindow when a chart
    // is displayed in a separate fullscreen window. A fresh ChartTabView is created
    // instead of moving the live ScottPlot WPF control between visual trees.
    public partial class ChartTabView
    {
        public ChartTabView CreateFullScreenClone()
        {
            return new ChartTabView(_symbol, new System.Collections.Generic.List<TradeIt.Models.MarketBar>(_bars));
        }
    }
}
