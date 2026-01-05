using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain;
using Nop.Services.Configuration;
using Nop.Services.Media;
using Nop.Web.Framework.Controllers;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.SimpleApi.Controllers
{
    [Route("api/simple/theme")]
    [ApiController]
    [IgnoreAntiforgeryToken]
    public class ThemeController : Nop.Web.Framework.Controllers.BasePluginController
    {
        private readonly ISettingService _settingService;
        private readonly IPictureService _pictureService;
        private readonly StoreInformationSettings _storeInformationSettings;

        public ThemeController(ISettingService settingService, 
            IPictureService pictureService, 
            StoreInformationSettings storeInformationSettings)
        {
            _settingService = settingService;
            _pictureService = pictureService;
            _storeInformationSettings = storeInformationSettings;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetTheme()
        {
            var themeSettings = await _settingService.LoadSettingAsync<MobileAppThemeSettings>();
            
            string logoUrl = themeSettings.CustomLogoUrl;

            if (themeSettings.UseStoreLogo && _storeInformationSettings.LogoPictureId > 0)
            {
                var url = await _pictureService.GetPictureUrlAsync(_storeInformationSettings.LogoPictureId);
                if (!string.IsNullOrEmpty(url))
                {
                    logoUrl = url;
                }
            }

            return Ok(new 
            {
                PrimaryColor = themeSettings.PrimaryColor,
                SecondaryColor = themeSettings.SecondaryColor,
                LogoUrl = logoUrl
            });
        }
    }
}
