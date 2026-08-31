using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TradeIt.Models;

namespace TradeIt.Services
{
    /// <summary>
    /// Stores user-defined symbol classification independently from Portfolio.Symbols.
    /// This is important because a portfolio may represent the whole data source and
    /// must not accidentally become an explicit symbol list just because a symbol was classified.
    /// </summary>
    public sealed class SymbolClassificationStore
    {
        private readonly string _filePath;
        private Dictionary<string, SymbolClassification> _items = new(StringComparer.OrdinalIgnoreCase);

        public SymbolClassificationStore()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TradeIt");
            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "SymbolClassifications.json");
            Load();
        }

        public void ApplyTo(IEnumerable<SymbolInfo> symbols)
        {
            foreach (SymbolInfo symbol in symbols)
            {
                if (symbol == null || string.IsNullOrWhiteSpace(symbol.FilePath))
                    continue;

                if (_items.TryGetValue(symbol.FilePath, out SymbolClassification? c))
                {
                    symbol.SecurityType = c.SecurityType;
                    symbol.Industry = c.Industry;
                    symbol.Group = c.Group;
                    symbol.SubGroup = c.SubGroup;
                }
            }
        }

        public void Save(IEnumerable<SymbolInfo> symbols)
        {
            foreach (SymbolInfo symbol in symbols)
            {
                if (symbol == null || string.IsNullOrWhiteSpace(symbol.FilePath))
                    continue;

                _items[symbol.FilePath] = new SymbolClassification
                {
                    SecurityType = symbol.SecurityType ?? "",
                    Industry = symbol.Industry ?? "",
                    Group = symbol.Group ?? "",
                    SubGroup = symbol.SubGroup ?? ""
                };
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(_filePath, JsonSerializer.Serialize(_items, options));
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return;

                var loaded = JsonSerializer.Deserialize<Dictionary<string, SymbolClassification>>(
                    File.ReadAllText(_filePath));

                if (loaded != null)
                    _items = new Dictionary<string, SymbolClassification>(loaded, StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                _items = new Dictionary<string, SymbolClassification>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private sealed class SymbolClassification
        {
            public string SecurityType { get; set; } = "";
            public string Industry { get; set; } = "";
            public string Group { get; set; } = "";
            public string SubGroup { get; set; } = "";
        }
    }
}
