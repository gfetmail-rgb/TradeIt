using System;

namespace TradeIt.Charts
{
    public static class ChartSettingsStore
    {
        private static ChartSettings _current =
            new ChartSettings();


        public static ChartSettings Current
        {
            get
            {
                return _current.Clone();
            }
        }


        public static event EventHandler? SettingsChanged;


        public static void Update(
            ChartSettings settings)
        {
            if (settings == null)
                return;

            _current =
                settings.Clone();

            SettingsChanged?.Invoke(
                null,
                EventArgs.Empty);
        }
    }
}