using Nop.Core.Domain.ScheduleTasks;
using Nop.Plugin.Misc.RestApi.Services;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.ScheduleTasks;
using Nop.Services.Security;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Misc.RestApi;

/// <summary>
/// Represents the REST API plugin (storefront API for apps, back-office API for third-party software)
/// </summary>
public class RestApiPlugin : BasePlugin, IMiscPlugin
{
    #region Fields

    protected readonly ILanguageService _languageService;
    protected readonly ILocalizationService _localizationService;
    protected readonly INopUrlHelper _nopUrlHelper;
    protected readonly IPermissionService _permissionService;
    protected readonly IScheduleTaskService _scheduleTaskService;
    protected readonly ISettingService _settingService;

    #endregion

    #region Ctor

    public RestApiPlugin(ILanguageService languageService,
        ILocalizationService localizationService,
        INopUrlHelper nopUrlHelper,
        IPermissionService permissionService,
        IScheduleTaskService scheduleTaskService,
        ISettingService settingService)
    {
        _languageService = languageService;
        _localizationService = localizationService;
        _nopUrlHelper = nopUrlHelper;
        _permissionService = permissionService;
        _scheduleTaskService = scheduleTaskService;
        _settingService = settingService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets a configuration page URL
    /// </summary>
    public override string GetConfigurationPageUrl()
    {
        return _nopUrlHelper.RouteUrl(RestApiDefaults.ConfigurationRouteName);
    }

    /// <summary>
    /// Install plugin
    /// </summary>
    public override async Task InstallAsync()
    {
        //both parts are disabled until the store owner enables them
        await _settingService.SaveSettingAsync(new RestApiSettings
        {
            StorefrontEnabled = false,
            BackOfficeEnabled = false,
            StorefrontTokenLifetimeDays = 30,
            BackOfficeTokenLifetimeMinutes = 60,
            MaxPageSize = 100,
            AppPrimaryColor = "#4AB2F1",
            AppSecondaryColor = "#3B464F",
            AppLogoPictureId = 0
        });

        if (await _scheduleTaskService.GetTaskByTypeAsync(RestApiDefaults.PurgeTokensTaskType) == null)
        {
            await _scheduleTaskService.InsertTaskAsync(new ScheduleTask
            {
                Enabled = true,
                LastEnabledUtc = DateTime.UtcNow,
                Seconds = 6 * 60 * 60,
                Name = RestApiDefaults.PurgeTokensTaskName,
                Type = RestApiDefaults.PurgeTokensTaskType
            });
        }

        await _localizationService.AddOrUpdateLocaleResourceAsync(RestApiLocales.English);
        foreach (var language in await _languageService.GetAllLanguagesAsync(showHidden: true))
        {
            if (language.LanguageCulture?.StartsWith("fr", StringComparison.OrdinalIgnoreCase) == true)
                await _localizationService.AddOrUpdateLocaleResourceAsync(RestApiLocales.French, language.Id);
        }

        await base.InstallAsync();
    }

    /// <summary>
    /// Uninstall plugin
    /// </summary>
    public override async Task UninstallAsync()
    {
        await _settingService.DeleteSettingAsync<RestApiSettings>();

        var task = await _scheduleTaskService.GetTaskByTypeAsync(RestApiDefaults.PurgeTokensTaskType);
        if (task != null)
            await _scheduleTaskService.DeleteTaskAsync(task);

        var permissionNames = new[]
        {
            RestApiPermissionConfigManager.MANAGE_API,
            RestApiPermissionConfigManager.USE_BACKOFFICE_API,
            RestApiPermissionConfigManager.USE_STOREFRONT_API
        };
        foreach (var record in (await _permissionService.GetAllPermissionRecordsAsync()).Where(record => permissionNames.Contains(record.SystemName)))
            await _permissionService.DeletePermissionRecordAsync(record);

        await _localizationService.DeleteLocaleResourcesAsync("Plugins.Misc.RestApi");
        foreach (var name in permissionNames)
            await _localizationService.DeleteLocaleResourcesAsync($"Security.Permission.{name}");

        await base.UninstallAsync();
    }

    #endregion
}
