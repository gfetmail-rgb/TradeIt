using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TradeIt.Models;

namespace TradeIt.Services
{
    public sealed class SymbolIdentityStore
    {
        private readonly string _filePath;
        private readonly object _sync = new();
        private List<SymbolIdentity> _items = new();

        public SymbolIdentityStore()
        {
            _filePath = StoragePaths.SymbolIdentitiesFile;
            Load();
        }

        public IReadOnlyList<SymbolIdentity> GetAll()
        {
            lock (_sync)
                return _items.Select(Clone).ToList();
        }

        public SymbolIdentity? FindBySymbolId12(string symbolId12)
        {
            if (string.IsNullOrWhiteSpace(symbolId12)) return null;
            lock (_sync)
                return _items.FirstOrDefault(x => string.Equals(x.SymbolId12, symbolId12.Trim(), StringComparison.OrdinalIgnoreCase)) is { } x ? Clone(x) : null;
        }

        public void Upsert(SymbolIdentity item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (string.IsNullOrWhiteSpace(item.SymbolId12))
                throw new ArgumentException("کد 12 رقمی نماد الزامی است.", nameof(item));

            lock (_sync)
            {
                int index = _items.FindIndex(x => string.Equals(x.SymbolId12, item.SymbolId12.Trim(), StringComparison.OrdinalIgnoreCase));
                item.SymbolId12 = item.SymbolId12.Trim();
                item.ModifiedAt = DateTime.Now;
                if (index >= 0) _items[index] = Clone(item);
                else _items.Add(Clone(item));
                Save();
            }
        }

        public bool Delete(string symbolId12)
        {
            lock (_sync)
            {
                int removed = _items.RemoveAll(x => string.Equals(x.SymbolId12, symbolId12?.Trim(), StringComparison.OrdinalIgnoreCase));
                if (removed > 0) Save();
                return removed > 0;
            }
        }

        public int DeleteMany(IEnumerable<string> ids)
        {
            HashSet<string> set = new(ids.Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
            lock (_sync)
            {
                int removed = _items.RemoveAll(x => set.Contains(x.SymbolId12));
                if (removed > 0) Save();
                return removed;
            }
        }

        public void DeleteAll()
        {
            lock (_sync)
            {
                _items.Clear();
                Save();
            }
        }

        public void ReplaceAll(IEnumerable<SymbolIdentity> items)
        {
            lock (_sync)
            {
                _items = items.Where(x => !string.IsNullOrWhiteSpace(x.SymbolId12))
                    .GroupBy(x => x.SymbolId12.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => Clone(g.Last()))
                    .ToList();
                Save();
            }
        }

        private void Load()
        {
            lock (_sync)
            {
                try
                {
                    if (!File.Exists(_filePath)) return;
                    string json = File.ReadAllText(_filePath);
                    _items = JsonSerializer.Deserialize<List<SymbolIdentity>>(json) ?? new();
                }
                catch
                {
                    _items = new();
                }
            }
        }

        private void Save()
        {
            string json = JsonSerializer.Serialize(_items, new JsonSerializerOptions { WriteIndented = true });
            string temp = _filePath + ".tmp";
            File.WriteAllText(temp, json);
            File.Move(temp, _filePath, true);
        }

        private static SymbolIdentity Clone(SymbolIdentity x) => new()
        {
            SymbolId12 = x.SymbolId12,
            SymbolCode5 = x.SymbolCode5,
            CompanyNameLatin = x.CompanyNameLatin,
            CompanyCode4 = x.CompanyCode4,
            CompanyName = x.CompanyName,
            SymbolNameFa = x.SymbolNameFa,
            SymbolName30Fa = x.SymbolName30Fa,
            CompanyId12 = x.CompanyId12,
            Market = x.Market,
            BoardCode = x.BoardCode,
            IndustryCode = x.IndustryCode,
            IndustryName = x.IndustryName,
            SubIndustryCode = x.SubIndustryCode,
            SubIndustryName = x.SubIndustryName,
            ModifiedAt = x.ModifiedAt
        };
    }
}