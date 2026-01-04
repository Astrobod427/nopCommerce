namespace Nop.Plugin.Api.SimpleApi.DTOs
{
    public class CurrencyDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CurrencyCode { get; set; }
        public string DisplayLocale { get; set; }
        public string CustomFormatting { get; set; }
        public decimal Rate { get; set; }
        public bool IsPrimaryExchangeRateCurrency { get; set; }
        public bool IsPrimaryStoreCurrency { get; set; }
    }
}
