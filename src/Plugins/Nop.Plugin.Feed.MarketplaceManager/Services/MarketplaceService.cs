using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Plugin.Feed.MarketplaceManager.Providers;
using Nop.Services.Plugins;
using System.Threading.Tasks; // Required for Task-based methods

namespace Nop.Plugin.Feed.MarketplaceManager.Services
{
    public class MarketplaceService
    {
        private readonly IPluginService _pluginService;

        public MarketplaceService(IPluginService pluginService)
        {
            _pluginService = pluginService;
        }

        public virtual async Task<IList<IMarketplaceProvider>> LoadActiveProvidersAsync()
        {
            // Load only installed and active plugins implementing IMarketplaceProvider
            var allProviders = await _pluginService.GetPluginsAsync<IMarketplaceProvider>(LoadPluginsMode.InstalledOnly);
            return allProviders.ToList();
        }
    }
}
