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


        // =========================================================
        // Constructor
        // =========================================================

        public PortfolioManager()
        {
            string appData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

            _folderPath =
                Path.Combine(
                    appData,
                    "TradeIt",
                    "Portfolios");

            Directory.CreateDirectory(
                _folderPath);
        }


        // =========================================================
        // Load All
        // =========================================================

        public List<Portfolio> LoadAll()
        {
            var portfolios =
                new List<Portfolio>();

            foreach (string file in
                     Directory.GetFiles(
                         _folderPath,
                         "*.json"))
            {
                try
                {
                    string json =
                        File.ReadAllText(file);

                    Portfolio? portfolio =
                        JsonSerializer.Deserialize<Portfolio>(
                            json);

                    if (portfolio != null)
                    {
                        if (portfolio.DataSource == null)
                        {
                            portfolio.DataSource =
                                new DataSource();
                        }

                        if (portfolio.Symbols == null)
                        {
                            portfolio.Symbols =
                                new List<SymbolInfo>();
                        }

                        portfolios.Add(
                            portfolio);
                    }
                }
                catch
                {
                    // فایل خراب نادیده گرفته می‌شود.
                }
            }

            return portfolios;
        }


        // =========================================================
        // Save
        // =========================================================

        public void Save(
            Portfolio portfolio)
        {
            if (portfolio == null)
            {
                throw new ArgumentNullException(
                    nameof(portfolio));
            }

            if (string.IsNullOrWhiteSpace(
                    portfolio.Name))
            {
                throw new ArgumentException(
                    "نام سبد مشخص نشده است.");
            }

            if (portfolio.DataSource == null)
            {
                portfolio.DataSource =
                    new DataSource();
            }

            if (portfolio.Symbols == null)
            {
                portfolio.Symbols =
                    new List<SymbolInfo>();
            }

            string safeName =
                MakeSafeFileName(
                    portfolio.Name);

            string filePath =
                Path.Combine(
                    _folderPath,
                    safeName + ".json");

            var options =
                new JsonSerializerOptions
                {
                    WriteIndented = true
                };

            string json =
                JsonSerializer.Serialize(
                    portfolio,
                    options);

            File.WriteAllText(
                filePath,
                json);
        }


        // =========================================================
        // Delete Portfolio
        // =========================================================

        public void Delete(
            string portfolioName)
        {
            string safeName =
                MakeSafeFileName(
                    portfolioName);

            string filePath =
                Path.Combine(
                    _folderPath,
                    safeName + ".json");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }


        // =========================================================
        // Safe File Name
        // =========================================================

        private static string MakeSafeFileName(
            string name)
        {
            foreach (char c in
                     Path.GetInvalidFileNameChars())
            {
                name =
                    name.Replace(
                        c,
                        '_');
            }

            return name.Trim();
        }
    }
}