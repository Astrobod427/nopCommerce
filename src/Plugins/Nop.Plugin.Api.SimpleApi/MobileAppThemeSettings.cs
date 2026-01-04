using Nop.Core.Configuration;

namespace Nop.Plugin.Api.SimpleApi
{
    public class MobileAppThemeSettings : ISettings
    {
        public string PrimaryColor { get; set; } = "#4AB2F1";
        public string SecondaryColor { get; set; } = "#3B464F";
        public bool UseStoreLogo { get; set; } = true;
        public string CustomLogoUrl { get; set; }
    }
}
