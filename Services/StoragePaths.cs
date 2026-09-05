using System;
using System.IO;

namespace TradeIt.Services
{
    internal static class StoragePaths
    {
        private static readonly string Root = AppContext.BaseDirectory;
        private static readonly string LegacyRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TradeIt");

        public static string PortfoliosDirectory => Path.Combine(Root, "Portfolios");
        public static string ChartSettingsFile => Path.Combine(Root, "ChartSettings.json");
        public static string SymbolIdentitiesFile => Path.Combine(Root, "symbol-identities.json");

        static StoragePaths()
        {
            Directory.CreateDirectory(PortfoliosDirectory);
            MigrateLegacyFiles();
        }

        private static void MigrateLegacyFiles()
        {
            try
            {
                string legacyPortfolios = Path.Combine(LegacyRoot, "Portfolios");
                if (Directory.Exists(legacyPortfolios))
                {
                    foreach (string file in Directory.GetFiles(legacyPortfolios, "*.json"))
                    {
                        string destination = Path.Combine(PortfoliosDirectory, Path.GetFileName(file));
                        if (!File.Exists(destination))
                            File.Copy(file, destination);
                    }
                }

                CopyIfMissing(Path.Combine(LegacyRoot, "ChartSettings.json"), ChartSettingsFile);
                CopyIfMissing(Path.Combine(LegacyRoot, "symbol-identities.json"), SymbolIdentitiesFile);
            }
            catch
            {
                // Storage migration must never prevent the application from starting.
            }
        }

        private static void CopyIfMissing(string source, string destination)
        {
            if (File.Exists(source) && !File.Exists(destination))
                File.Copy(source, destination);
        }
    }
}
