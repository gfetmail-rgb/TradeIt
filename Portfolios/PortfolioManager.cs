using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using TradeIt.Models;

namespace TradeIt.Portfolios
{
    public class PortfolioManager
    {
        private readonly string _folderPath;

        public PortfolioManager()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _folderPath = Path.Combine(appData, "TradeIt", "Portfolios");
            Directory.CreateDirectory(_folderPath);
        }

        public List<Portfolio> LoadAll()
        {
            var portfolios = new List<Portfolio>();

            foreach (string file in Directory.GetFiles(_folderPath, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    Portfolio? portfolio = JsonSerializer.Deserialize<Portfolio>(json);

                    if (portfolio != null)
                    {
                        if (portfolio.DataSource == null)
                            portfolio.DataSource = new DataSource();

                        if (portfolio.Symbols == null)
                            portfolio.Symbols = new List<SymbolInfo>();

                        portfolios.Add(portfolio);
                    }
                }
                catch
                {
                    // فایل خراب نادیده گرفته می‌شود.
                }
            }

            return portfolios;
        }

        public void Save(Portfolio portfolio)
        {
            if (portfolio == null)
                throw new ArgumentNullException(nameof(portfolio));

            if (string.IsNullOrWhiteSpace(portfolio.Name))
                throw new ArgumentException("نام سبد مشخص نشده است.");

            if (portfolio.DataSource == null)
                portfolio.DataSource = new DataSource();

            if (portfolio.Symbols == null)
                portfolio.Symbols = new List<SymbolInfo>();

            // هر سبدی که فهرست صریح نماد دارد باید هنگام بارگذاری
            // همان فهرست را ملاک قرار دهد. این موضوع برای Make Watch
            // و حذف نمادها ضروری است، در حالی که سبدهای معمولی که
            // Symbols آن‌ها خالی است همچنان از کل DataSource استفاده می‌کنند.
            if (portfolio.Symbols.Count > 0)
                portfolio.UseExplicitSymbolList = true;

            string safeName = MakeSafeFileName(portfolio.Name);
            string filePath = Path.Combine(_folderPath, safeName + ".json");

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(portfolio, options);
            File.WriteAllText(filePath, json);
        }

        public void Delete(string portfolioName)
        {
            string safeName = MakeSafeFileName(portfolioName);
            string filePath = Path.Combine(_folderPath, safeName + ".json");

            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        private static string MakeSafeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            return name.Trim();
        }
    }
}