using Nop.Core.Configuration;

namespace Nop.Plugin.Feed.Ricardo
{
    public class RicardoSettings : ISettings
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public bool UseSandbox { get; set; } = true;
        public bool AutoSyncOnCreate { get; set; } = true;
    }
}
