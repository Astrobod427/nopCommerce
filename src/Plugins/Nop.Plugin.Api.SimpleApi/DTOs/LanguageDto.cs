namespace Nop.Plugin.Api.SimpleApi.DTOs
{
    public class LanguageDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LanguageCulture { get; set; }
        public string UniqueSeoCode { get; set; }
        public string FlagImageFileName { get; set; }
        public bool Published { get; set; }
    }
}
