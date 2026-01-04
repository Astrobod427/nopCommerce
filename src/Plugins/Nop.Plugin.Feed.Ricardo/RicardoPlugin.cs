using System.Collections.Generic;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Plugins;
using Nop.Web.Framework.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Nop.Plugin.Feed.Ricardo.Services;

using Nop.Core;
using Nop.Services.Localization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Plugin.Feed.MarketplaceManager.Providers;
using Nop.Plugin.Feed.Ricardo.Services;
using Nop.Core.Domain.Catalog;

namespace Nop.Plugin.Feed.Ricardo
{
    public class RicardoPlugin : BasePlugin, IMarketplaceProvider
    {
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly ILocalizationService _localizationService;
        private readonly RicardoApiService _ricardoApiService;

        public RicardoPlugin(ISettingService settingService, IWebHelper webHelper, 
            ILocalizationService localizationService, RicardoApiService ricardoApiService)
        {
            _settingService = settingService;
            _webHelper = webHelper;
            _localizationService = localizationService;
            _ricardoApiService = ricardoApiService;
        }

        public override async Task InstallAsync()
        {
            var settings = new RicardoSettings
            {
                UseSandbox = true,
                AutoSyncOnCreate = true
            };
            await _settingService.SaveSettingAsync(settings);

            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            await _settingService.DeleteSettingAsync<RicardoSettings>();
            
            await base.UninstallAsync();
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/Ricardo/Configure";
        }

        // IMarketplaceProvider implementation
        public string SystemName => "Feed.Ricardo"; // Matches plugin.json
        public string FriendlyName => "Ricardo.ch";

        public async Task<string> ListProductAsync(Product product)
        {
            var success = await _ricardoApiService.CreateArticleAsync(product);
            return success ? product.Id.ToString() : null; // Return product ID as external ID
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            // TODO: Implement update logic for Ricardo
            return await Task.FromResult(true);
        }

        public async Task<int> ImportOrdersAsync()
        {
            // TODO: Implement order import for Ricardo
            return await Task.FromResult(0);
        }
    }
}
