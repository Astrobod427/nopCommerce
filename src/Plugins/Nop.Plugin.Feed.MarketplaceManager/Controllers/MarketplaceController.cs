using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Feed.MarketplaceManager.Models;
using Nop.Plugin.Feed.MarketplaceManager.Services;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Feed.MarketplaceManager.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    public class MarketplaceController : BasePluginController
    {
        private readonly MarketplaceService _marketplaceService;
        private readonly IPermissionService _permissionService;

        public MarketplaceController(MarketplaceService marketplaceService, IPermissionService permissionService)
        {
            _marketplaceService = marketplaceService;
            _permissionService = permissionService;
        }

        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PLUGINS))
                return AccessDeniedView();

            var model = new ConfigurationModel();
            var providers = await _marketplaceService.LoadActiveProvidersAsync();
            foreach(var p in providers)
            {
                model.ActiveProviders.Add(new ProviderModel {
                    SystemName = p.SystemName,
                    FriendlyName = p.FriendlyName
                });
            }

            return View("~/Plugins/Feed.MarketplaceManager/Views/Configure.cshtml", model);
        }
    }
}
