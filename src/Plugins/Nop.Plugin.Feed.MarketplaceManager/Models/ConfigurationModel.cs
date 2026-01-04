using Nop.Web.Framework.Models;
using System.Collections.Generic;

namespace Nop.Plugin.Feed.MarketplaceManager.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        public ConfigurationModel()
        {
            ActiveProviders = new List<ProviderModel>();
        }

        public IList<ProviderModel> ActiveProviders { get; set; }
    }

    public record ProviderModel : BaseNopModel
    {
        public string SystemName { get; set; }
        public string FriendlyName { get; set; }
    }
}
