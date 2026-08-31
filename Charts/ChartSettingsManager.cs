using System;
using System.IO;
using System.Text.Json;

namespace TradeIt.Charts
{
    public static class ChartSettingsManager
    {
        private static readonly object Sync = new();
        private static readonly string SettingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TradeIt");
        private static readonly string SettingsFile = Path.Combine(SettingsDirectory, "ChartSettings.json");
        private static ChartSettings _current = LoadOrCreateDefaults();

        public static event EventHandler? SettingsChanged;
        public static ChartSettings Current => _current.Clone();

        public static void SetCurrent(ChartSettings settings)
        {
            if (settings == null) return;
            lock (Sync) _current = settings.Clone();
            SettingsChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void SetDefaults(ChartSettings settings) => SetCurrent(settings);
        public static ChartSettings Clone(ChartSettings settings) => settings?.Clone() ?? new ChartSettings();

        public static void Save(ChartSettings settings)
        {
            if (settings == null) return;
            lock (Sync)
            {
                _current = settings.Clone();
                _current.HasUserSavedSettings = true;
                Directory.CreateDirectory(SettingsDirectory);
                File.WriteAllText(SettingsFile, JsonSerializer.Serialize(_current, new JsonSerializerOptions { WriteIndented = true }));
            }
            SettingsChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void Save() => Save(_current);

        private static ChartSettings LoadOrCreateDefaults()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    var saved = JsonSerializer.Deserialize<ChartSettings>(File.ReadAllText(SettingsFile));
                    if (saved != null && saved.HasUserSavedSettings) return saved;
                }
            }
            catch { }

            return new ChartSettings
            {
                GridVisible = false,
                CrosshairColor = "#909090",
                CrosshairLineWidth = 1,
                CrosshairPattern = "Dotted",
                HasUserSavedSettings = false
            };
        }
    }
}