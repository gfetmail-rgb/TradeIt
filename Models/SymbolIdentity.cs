using System;

namespace TradeIt.Models
{
    public class SymbolIdentity
    {
        public string SymbolId12 { get; set; } = "";
        public string SymbolCode5 { get; set; } = "";
        public string CompanyNameLatin { get; set; } = "";
        public string CompanyCode4 { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string SymbolNameFa { get; set; } = "";
        public string SymbolName30Fa { get; set; } = "";
        public string CompanyId12 { get; set; } = "";
        public string Market { get; set; } = "";
        public string BoardCode { get; set; } = "";
        public string IndustryCode { get; set; } = "";
        public string IndustryName { get; set; } = "";
        public string SubIndustryCode { get; set; } = "";
        public string SubIndustryName { get; set; } = "";

        public DateTime ModifiedAt { get; set; } = DateTime.Now;
    }
}