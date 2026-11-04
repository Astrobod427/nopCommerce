using Nop.Core.Domain.Logging;
using Nop.Core.Domain.ScheduleTasks;
using Nop.Data;
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

    protected readonly IRepository<ActivityLogType> _activityLogTypeRepository;
    protected readonly ILanguageService _languageService;
    protected readonly ILocalizationService _localizationService;
    protected readonly INopUrlHelper _nopUrlHelper;
    protected readonly IPermissionService _permissionService;
    protected readonly IScheduleTaskService _scheduleTaskService;
    protected readonly ISettingService _settingService;

    #endregion

    #region Ctor

    public RestApiPlugin(IRepository<ActivityLogType> activityLogTypeRepository,
        ILanguageService languageService,
        ILocalizationService localizationService,
        INopUrlHelper nopUrlHelper,
        IPermissionService permissionService,
        IScheduleTaskService scheduleTaskService,
        ISettingService settingService)
    {
        _activityLogTypeRepository = activityLogTypeRepository;
        _languageService = languageService;
        _localizationService = localizationService;
        _nopUrlHelper = nopUrlHelper;
        _permissionService = permissionService;
        _scheduleTaskService = scheduleTaskService;
        _settingService = settingService;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Creates the activity log types of the plugin (sign-in failures, back-office writes, refused requests)
    /// </summary>
    protected virtual async Task InstallActivityLogTypesAsync()
    {
        var types = new Dictionary<string, string>
        {
            [RestApiDefaults.ActivityLogSignInFailed] = "REST API: failed sign-in",
            [RestApiDefaults.ActivityLogBackOfficeWrite] = "REST API: back-office change",
            [RestApiDefaults.ActivityLogRequestRefused] = "REST API: refused request"
        };

        var existing = _activityLogTypeRepository.Table.Select(type => type.SystemKeyword).ToList();
        foreach (var (keyword, name) in types.Where(type => !existing.Contains(type.Key)))
            await _activityLogTypeRepository.InsertAsync(new ActivityLogType { SystemKeyword = keyword, Name = name, Enabled = true });
    }

    /// <summary>
    /// Adds the plugin texts (English, and French for every French language)
    /// </summary>
    protected virtual async Task InstallLocalesAsync()
    {
        await _localizationService.AddOrUpdateLocaleResourceAsync(RestApiLocales.English);
        foreach (var language in await _languageService.GetAllLanguagesAsync(showHidden: true))
        {
            if (language.LanguageCulture?.StartsWith("fr", StringComparison.OrdinalIgnoreCase) == true)
                await _localizationService.AddOrUpdateLocaleResourceAsync(RestApiLocales.French, language.Id);
        }
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
            RequestsPerMinute = 300,
            AuthRequestsPerMinute = 10,
            CorsAllowedOrigins = string.Empty,
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

        await InstallLocalesAsync();
        await InstallActivityLogTypesAsync();

        await base.InstallAsync();
    }

    /// <summary>
    /// Update plugin (new texts and activity log types; schema and settings are in the update migrations)
    /// </summary>
    public override async Task UpdateAsync(string currentVersion, string targetVersion)
    {
        await InstallLocalesAsync();
        await InstallActivityLogTypesAsync();

        //the update migrations wrote new settings directly in the database, after the settings were cached
        await _settingService.ClearCacheAsync();

        await base.UpdateAsync(currentVersion, targetVersion);
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

        var keywords = new[] { RestApiDefaults.ActivityLogSignInFailed, RestApiDefaults.ActivityLogBackOfficeWrite, RestApiDefaults.ActivityLogRequestRefused };
        await _activityLogTypeRepository.DeleteAsync(type => keywords.Contains(type.SystemKeyword));

        await _localizationService.DeleteLocaleResourcesAsync("Plugins.Misc.RestApi");
        foreach (var name in permissionNames)
            await _localizationService.DeleteLocaleResourcesAsync($"Security.Permission.{name}");

        await base.UninstallAsync();
    }

    #endregion
}
