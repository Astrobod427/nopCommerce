using Nop.Services.Plugins;
using Nop.Services.Configuration;
using System;
using System.Threading.Tasks;
using Nop.Core;

namespace Nop.Plugin.Api.SimpleApi
{
    public class SimpleApiPlugin : BasePlugin
    {
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;

        public SimpleApiPlugin(ISettingService settingService, IWebHelper webHelper)
        {
            _settingService = settingService;
            _webHelper = webHelper;
        }

        public override string GetConfigurationPageUrl()
        {
            return _webHelper.GetStoreLocation() + "Admin/SimpleApiAdmin/Configure";
        }

        public override async Task InstallAsync()
        {
            var settings = new SimpleApiSettings
            {
                ApiKey = Guid.NewGuid().ToString()
            };
            await _settingService.SaveSettingAsync(settings);

            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            await _settingService.DeleteSettingAsync<SimpleApiSettings>();
            await base.UninstallAsync();
        }
    }
}
