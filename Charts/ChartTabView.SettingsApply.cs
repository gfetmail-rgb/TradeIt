namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        /// <summary>
        /// Re-applies the current persisted settings to the already open chart.
        /// </summary>
        public void ApplySettingsImmediately()
        {
            ApplyStoredChartSettings();
        }
    }
}
