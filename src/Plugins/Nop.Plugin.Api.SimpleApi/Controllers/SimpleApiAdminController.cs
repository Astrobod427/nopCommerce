using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Api.SimpleApi.Models;
using Nop.Services.Configuration;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.SimpleApi.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    public class SimpleApiAdminController : BasePluginController
    {
        private readonly ISettingService _settingService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;

        public SimpleApiAdminController(ISettingService settingService,
            INotificationService notificationService,
            IPermissionService permissionService)
        {
            _settingService = settingService;
            _notificationService = notificationService;
            _permissionService = permissionService;
        }

        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_WIDGETS))
                return AccessDeniedView();

            var settings = await _settingService.LoadSettingAsync<SimpleApiSettings>();
            var model = new ConfigurationModel
            {
                ApiKey = settings.ApiKey
            };

            return View("~/Plugins/Api.SimpleApi/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_WIDGETS))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await Configure();

            var settings = await _settingService.LoadSettingAsync<SimpleApiSettings>();
            settings.ApiKey = model.ApiKey;

            await _settingService.SaveSettingAsync(settings);
            _notificationService.SuccessNotification("Configuration updated");

            return await Configure();
        }
    }
}
