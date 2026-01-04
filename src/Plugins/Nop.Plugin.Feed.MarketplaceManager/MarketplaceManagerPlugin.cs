using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Plugin.Feed.MarketplaceManager.Services;
using Nop.Services.Plugins;
using Nop.Core;

namespace Nop.Plugin.Feed.MarketplaceManager
{
    public class MarketplaceManagerPlugin : BasePlugin
    {
        private readonly IWebHelper _webHelper;
        public MarketplaceManagerPlugin(IWebHelper webHelper) 
        {
            _webHelper = webHelper;
        }

        public override async Task InstallAsync()
        {
            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            await base.UninstallAsync();
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/Marketplace/Configure";
        }
    }
}
