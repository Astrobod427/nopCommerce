using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Feed.Ricardo.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Feed.Ricardo.ClientId")]
        public string ClientId { get; set; }

        [NopResourceDisplayName("Plugins.Feed.Ricardo.ClientSecret")]
        public string ClientSecret { get; set; }

        [NopResourceDisplayName("Plugins.Feed.Ricardo.UseSandbox")]
        public bool UseSandbox { get; set; }

        [NopResourceDisplayName("Plugins.Feed.Ricardo.AutoSyncOnCreate")]
        public bool AutoSyncOnCreate { get; set; }
    }
}
