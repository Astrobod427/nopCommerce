using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Feed.Ricardo.Models;
using Nop.Services.Configuration;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Feed.Ricardo.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    public class RicardoController : BasePluginController
    {
        private readonly ISettingService _settingService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;

        public RicardoController(ISettingService settingService,
            INotificationService notificationService,
            IPermissionService permissionService)
        {
            _settingService = settingService;
            _notificationService = notificationService;
            _permissionService = permissionService;
        }

        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PLUGINS))
                return AccessDeniedView();

            var settings = await _settingService.LoadSettingAsync<RicardoSettings>();
            var model = new ConfigurationModel
            {
                ClientId = settings.ClientId,
                ClientSecret = settings.ClientSecret,
                UseSandbox = settings.UseSandbox,
                AutoSyncOnCreate = settings.AutoSyncOnCreate
            };

            return View("~/Plugins/Feed.Ricardo/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PLUGINS))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await Configure();

            var settings = await _settingService.LoadSettingAsync<RicardoSettings>();
            settings.ClientId = model.ClientId;
            settings.ClientSecret = model.ClientSecret;
            settings.UseSandbox = model.UseSandbox;
            settings.AutoSyncOnCreate = model.AutoSyncOnCreate;

            await _settingService.SaveSettingAsync(settings);
            _notificationService.SuccessNotification("Ricardo.ch configuration updated");

            return await Configure();
        }
    }
}
