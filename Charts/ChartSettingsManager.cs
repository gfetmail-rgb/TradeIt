using System;
using System.IO;
using System.Text.Json;

namespace TradeIt.Charts
{
    public static class ChartSettingsManager
    {
        private static readonly object Sync = new();
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TradeIt");
        private static readonly string SettingsFile = Path.Combine(SettingsDirectory, "ChartSettings.json");

        private static ChartSettings _current = LoadOrCreateDefaults();

        public static ChartSettings Current => _current.Clone();

        public static void SetCurrent(ChartSettings settings)
        {
            if (settings == null) return;
            lock (Sync) _current = settings.Clone();
        }

        public static void SetDefaults(ChartSettings settings)
        {
            SetCurrent(settings);
        }

        public static ChartSettings Clone(ChartSettings settings)
        {
            return settings?.Clone() ?? new ChartSettings();
        }

        public static void Save(ChartSettings settings)
        {
            if (settings == null) return;
            lock (Sync)
            {
                _current = settings.Clone();
                _current.HasUserSavedSettings = true;
                Directory.CreateDirectory(SettingsDirectory);
                string json = JsonSerializer.Serialize(_current, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            }
        }

        public static void Save() => Save(_current);

        private static ChartSettings LoadOrCreateDefaults()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    string json = File.ReadAllText(SettingsFile);
                    ChartSettings? saved = JsonSerializer.Deserialize<ChartSettings>(json);
                    if (saved != null && saved.HasUserSavedSettings) return saved;
                }
            }
            catch { }

            var firstRun = new ChartSettings
            {
                GridVisible = false,
                CrosshairColor = "#909090",
                CrosshairLineWidth = 1,
                CrosshairPattern = "Dotted",
                HasUserSavedSettings = false
            };

            return firstRun;
        }
    }
}