using System;
using System.IO;
using System.Text.Json;
using TradeIt.Services;

namespace TradeIt.Charts
{
    public static class ChartSettingsManager
    {
        private static readonly object Sync = new();
        private static readonly string SettingsFile = StoragePaths.ChartSettingsFile;
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
        private static ChartSettings _current = LoadOrCreateDefaults();

        public static event EventHandler? SettingsChanged;
        public static ChartSettings Current
        {
            get
            {
                lock (Sync)
                    return _current.Clone();
            }
        }

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
            lock (Sync) _current = settings.Clone();
            PersistCurrent();
            SettingsChanged?.Invoke(null, EventArgs.Empty);
        }

        // Drawing-tool preferences are global user preferences. Persist them without broadcasting
        // a full chart-settings redraw to every open chart.
        public static void SaveDrawingToolStyles(ChartSettings settings)
        {
            if (settings == null) return;
            lock (Sync) _current = settings.Clone();
            PersistCurrent();
        }

        private static void PersistCurrent()
        {
            ChartSettings snapshot;
            lock (Sync)
            {
                _current.HasUserSavedSettings = true;
                snapshot = _current.Clone();
            }

            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile)!);
            File.WriteAllText(SettingsFile, JsonSerializer.Serialize(snapshot, JsonOptions));
        }

        public static void Save() => Save(Current);

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
