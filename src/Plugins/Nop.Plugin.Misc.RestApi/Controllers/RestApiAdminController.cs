using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Misc.RestApi.Domain;
using Nop.Plugin.Misc.RestApi.Models;
using Nop.Plugin.Misc.RestApi.Services;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.RestApi.Controllers;

/// <summary>
/// Admin pages of the plugin: settings, API clients, active sessions
/// </summary>
[Area(AreaNames.ADMIN)]
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
public class RestApiAdminController : BasePluginController
{
    #region Constants

    protected const string VIEW_PATH = "~/Plugins/Misc.RestApi/Views/";

    #endregion

    #region Fields

    protected readonly ApiTokenService _apiTokenService;
    protected readonly ICustomerActivityService _customerActivityService;
    protected readonly ICustomerService _customerService;
    protected readonly IDateTimeHelper _dateTimeHelper;
    protected readonly ILocalizationService _localizationService;
    protected readonly INotificationService _notificationService;
    protected readonly IPermissionService _permissionService;
    protected readonly ISettingService _settingService;
    protected readonly IStoreContext _storeContext;
    protected readonly RestApiSettings _settings;

    #endregion

    #region Ctor

    public RestApiAdminController(ApiTokenService apiTokenService,
        ICustomerActivityService customerActivityService,
        ICustomerService customerService,
        IDateTimeHelper dateTimeHelper,
        ILocalizationService localizationService,
        INotificationService notificationService,
        IPermissionService permissionService,
        ISettingService settingService,
        IStoreContext storeContext,
        RestApiSettings settings)
    {
        _apiTokenService = apiTokenService;
        _customerActivityService = customerActivityService;
        _customerService = customerService;
        _dateTimeHelper = dateTimeHelper;
        _localizationService = localizationService;
        _notificationService = notificationService;
        _permissionService = permissionService;
        _settingService = settingService;
        _storeContext = storeContext;
        _settings = settings;
    }

    #endregion

    #region Utilities

    protected virtual async Task<DateTime> ToUserTimeAsync(DateTime utc)
    {
        return await _dateTimeHelper.ConvertToUserTimeAsync(utc, DateTimeKind.Utc);
    }

    protected virtual async Task<DateTime?> ToUserTimeAsync(DateTime? utc)
    {
        return utc.HasValue ? await ToUserTimeAsync(utc.Value) : null;
    }

    protected virtual async Task<int> CountSessionsAsync(ApiTokenKind? kind = null, int? apiClientId = null)
    {
        return (await _apiTokenService.SearchTokensAsync(kind, apiClientId, pageSize: 1)).TotalCount;
    }

    protected virtual async Task<ApiClientModel> PrepareClientModelAsync(ApiClientModel model, ApiClient client)
    {
        if (client == null)
            return model;

        var customer = await _customerService.GetCustomerByIdAsync(client.CustomerId);
        model.Id = client.Id;
        model.Name ??= client.Name;
        model.CustomerEmail ??= customer?.Email;
        model.ClientId = client.ClientId;
        model.CustomerId = client.CustomerId;
        model.CustomerHasPermission = customer != null && await _permissionService.AuthorizeAsync(RestApiPermissionConfigManager.USE_BACKOFFICE_API, customer);
        model.ActiveSessions = await CountSessionsAsync(apiClientId: client.Id);

        return model;
    }

    /// <summary>
    /// Finds the account an API client acts for: an existing, active, registered customer
    /// </summary>
    protected virtual async Task<Customer> GetClientCustomerAsync(string email)
    {
        var customer = string.IsNullOrWhiteSpace(email) ? null : await _customerService.GetCustomerByEmailAsync(email.Trim());
        if (customer == null || customer.Deleted || !customer.Active || !await _customerService.IsRegisteredAsync(customer))
            return null;

        return customer;
    }

    #endregion

    #region Settings

    [CheckPermission(RestApiPermissionConfigManager.MANAGE_API)]
    public virtual async Task<IActionResult> Configure()
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var model = new ConfigurationModel
        {
            StorefrontEnabled = _settings.StorefrontEnabled,
            BackOfficeEnabled = _settings.BackOfficeEnabled,
            StorefrontTokenLifetimeDays = _settings.StorefrontTokenLifetimeDays,
            BackOfficeTokenLifetimeMinutes = _settings.BackOfficeTokenLifetimeMinutes,
            MaxPageSize = _settings.MaxPageSize,
            AppPrimaryColor = _settings.AppPrimaryColor,
            AppSecondaryColor = _settings.AppSecondaryColor,
            AppLogoPictureId = _settings.AppLogoPictureId,
            ApiBaseUrl = $"{store.Url.TrimEnd('/')}/{RestApiDefaults.ROUTE_PREFIX}",
            ActiveStorefrontSessions = await CountSessionsAsync(ApiTokenKind.Storefront),
            ActiveBackOfficeSessions = await CountSessionsAsync(ApiTokenKind.BackOffice)
        };

        return View(VIEW_PATH + "Configure.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(RestApiPermissionConfigManager.MANAGE_API)]
    public virtual async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!ModelState.IsValid)
            return await Configure();

        _settings.StorefrontEnabled = model.StorefrontEnabled;
        _settings.BackOfficeEnabled = model.BackOfficeEnabled;
        _settings.StorefrontTokenLifetimeDays = Math.Clamp(model.StorefrontTokenLifetimeDays, 1, 365);
        _settings.BackOfficeTokenLifetimeMinutes = Math.Clamp(model.BackOfficeTokenLifetimeMinutes, 5, 24 * 60);
        _settings.MaxPageSize = Math.Clamp(model.MaxPageSize, 1, 1000);
        _settings.AppPrimaryColor = model.AppPrimaryColor?.Trim();
        _settings.AppSecondaryColor = model.AppSecondaryColor?.Trim();
        _settings.AppLogoPictureId = model.AppLogoPictureId;
        await _settingService.SaveSettingAsync(_settings);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return RedirectToAction(nameof(Configure));
    }

    #endregion

    #region API clients

    [CheckPermission(RestApiPermissionConfigManager.MANAGE_API)]
    public virtual async Task<IActionResult> Clients()
    {
        var items = new List<ApiClientListItemModel>();
        foreach (var client in await _apiTokenService.GetAllClientsAsync())
        {
            var model = await PrepareClientModelAsync(new ApiClientModel(), client);
            items.Add(new ApiClientListItemModel
            {
                Id = client.Id,
                Name = client.Name,
                ClientId = client.ClientId,
                CustomerEmail = model.CustomerEmail,
                CustomerHasPermission = model.CustomerHasPermission,
                Active = client.Active,
                ActiveSessions = model.ActiveSessions,
                CreatedOn = await ToUserTimeAsync(client.CreatedOnUtc),
                LastUsedOn = await ToUserTimeAsync(client.LastUsedOnUtc)
            });
        }

        return View(VIEW_PATH + "Clients.cshtml", new ApiClientListModel { Clients = items });
    }

    [CheckPermission(RestApiPermissionConfigManager.MANAGE_API)]
    public virtual IActionResult CreateClient()
    {
        return View(VIEW_PATH + "ClientEdit.cshtml", new ApiClientModel { Active = true });
    }

    [HttpPost]
    [CheckPermission(RestApiPermissionConfigManager.MANAGE_API)]
    public virtual async Task<IActionResult> CreateClient(ApiClientModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            ModelState.AddModelError(nameof(model.Name), await _localizationService.GetResourceAsync("Plugins.Misc.RestApi.Clients.Fields.Name.Required"));

        var customer = await GetClientCustomerAsync(model.CustomerEmail);
        if (customer == null)
            ModelState.AddModelError(nameof(model.CustomerEmail), await _localizationService.GetResourceAsync("Plugins.Misc.RestApi.Clients.Fields.CustomerEmail.Invalid"));

        if (!ModelState.IsValid)
            return View(VIEW_PATH + "ClientEdit.cshtml", model);

        var (client, secret) = await _apiTokenService.CreateClientAsync(model.Name.Trim(), customer!.Id, model.Active);
        await _customerActivityService.InsertActivityAsync("EditSettings", $"REST API: client '{client.Name}' created for {customer.Email}");

        var result = await PrepareClientModelAsync(new ApiClientModel { Active = client.Active, NewSecret = secret }, client);
        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Misc.RestApi.Clients.Created"));

        //the secret is shown once: render the page directly instead of redirecting
        return View(VIEW_PATH + "ClientEdit.cshtml", result);
    }

    [CheckPermission(RestApiPermissionConfigManager.MANAGE_API)]
    public virtual async Task<IActionResult> EditClient(int id)
    {
        var client = await _apiTokenService.GetClientByIdAsync(id);
        if (client == null)
            return RedirectToAction(nameof(Clients));

        return View(VIEW_PATH + "ClientEdit.cshtml", await PrepareClientModelAsync(new ApiClientModel { Active = client.Active }, client));
    }

    [HttpPost]
    [CheckPermission(RestApiPermissionConfigManager.MANAGE_API)]
    public virtual async Task<IActionResult> EditClient(ApiClientModel model)
    {
        var client = await _apiTokenService.GetClientByIdAsync(model.Id);
        if (client == null)
            return RedirectToAction(nameof(Clients));

        if (string.IsNullOrWhiteSpace(model.Name))
            ModelState.AddModelError(nameof(model.Name), await _localizationService.GetResourceAsync("Plugins.Misc.RestApi.Clients.Fields.Name.Required"));

        var customer = await GetClientCustomerAsync(model.CustomerEmail);
        if (customer == null)
            ModelState.AddModelError(nameof(model.CustomerEmail), await _localizationService.GetResourceAsync("Plugins.Misc.RestApi.Clients.Fields.CustomerEmail.Invalid"));

        if (!ModelState.IsValid)
            return View(VIEW_PATH + "ClientEdit.cshtml", await PrepareClientModelAsync(model, client));

        //a new account or a deactivation ends the current sessions of the client
        var revoke = client.CustomerId != customer!.Id || (client.Active && !model.Active);
        client.Name = model.Name.Trim();
        client.CustomerId = customer.Id;
        client.Active = model.Active;
        await _apiTokenService.UpdateClientAsync(client);
        if (revoke)
            await _apiTokenService.RevokeTokensAsync(apiClientId: client.Id);

        await _customerActivityService.InsertActivityAsync("EditSettings", $"REST API: client '{client.Name}' updated");
        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Misc.RestApi.Clients.Updated"));

        return RedirectToAction(nameof(EditClient), new { id = client.Id });
    }

    [HttpPost]
    [CheckPermission(RestApiPermissionConfigManager.MANAGE_API)]
    public virtual async Task<IActionResult> RegenerateSecret(int id)
    {
        var client = await _apiTokenService.GetClientByIdAsync(id);
        if (client == null)
            return RedirectToAction(nameof(Clients));

        var secret = await _apiTokenService.RegenerateSecretAsync(client);
        await _customerActivityService.InsertActivityAsync("EditSettings", $"REST API: secret of client '{client.Name}' regenerated");
        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Misc.RestApi.Clients.SecretRegenerated"));

        return View(VIEW_PATH + "ClientEdit.cshtml", await PrepareClientModelAsync(new ApiClientModel { Active = client.Active, NewSecret = secret }, client));
    }

    [HttpPost]
    [CheckPermission(RestApiPermissionConfigManager.MANAGE_API)]
    public virtual async Task<IActionResult> DeleteClient(int id)
    {
        var client = await _apiTokenService.GetClientByIdAsync(id);
        if (client != null)
        {
            await _apiTokenService.DeleteClientAsync(client);
            await _customerActivityService.InsertActivityAsync("EditSettings", $"REST API: client '{client.Name}' deleted");
            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Misc.RestApi.Clients.Deleted"));
        }

        return RedirectToAction(nameof(Clients));
    }

    #endregion

    #region Sessions

    [CheckPermission(RestApiPermissionConfigManager.MANAGE_API)]
    public virtual async Task<IActionResult> Sessions(int? apiClientId, int? customerId)
    {
        var tokens = await _apiTokenService.SearchTokensAsync(apiClientId: apiClientId, customerId: customerId, pageSize: 200);
        var clients = (await _apiTokenService.GetAllClientsAsync()).ToDictionary(c => c.Id, c => c.Name);

        var sessions = new List<ApiSessionModel>();
        foreach (var token in tokens)
        {
            var customer = await _customerService.GetCustomerByIdAsync(token.CustomerId);
            sessions.Add(new ApiSessionModel
            {
                Id = token.Id,
                Kind = await _localizationService.GetResourceAsync($"Plugins.Misc.RestApi.Sessions.Kind.{token.Kind}"),
                CustomerId = token.CustomerId,
                CustomerEmail = customer == null ? null : customer.Email ?? await _localizationService.GetResourceAsync("Admin.Customers.Guest"),
                ClientName = token.ApiClientId.HasValue && clients.TryGetValue(token.ApiClientId.Value, out var name) ? name : null,
                IpAddress = token.IpAddress,
                CreatedOn = await ToUserTimeAsync(token.CreatedOnUtc),
                LastUsedOn = await ToUserTimeAsync(token.LastUsedOnUtc),
                ExpiresOn = await ToUserTimeAsync(token.ExpiresOnUtc)
            });
        }

        return View(VIEW_PATH + "Sessions.cshtml", new ApiSessionListModel { Sessions = sessions, TotalCount = tokens.TotalCount });
    }

    [HttpPost]
    [CheckPermission(RestApiPermissionConfigManager.MANAGE_API)]
    public virtual async Task<IActionResult> RevokeSession(int id)
    {
        var token = await _apiTokenService.GetTokenByIdAsync(id);
        if (token != null)
            await _apiTokenService.RevokeTokenAsync(token);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Misc.RestApi.Sessions.Revoked"));

        return RedirectToAction(nameof(Sessions));
    }

    [HttpPost]
    [CheckPermission(RestApiPermissionConfigManager.MANAGE_API)]
    public virtual async Task<IActionResult> RevokeClientSessions(int id)
    {
        var count = await _apiTokenService.RevokeTokensAsync(apiClientId: id);
        _notificationService.SuccessNotification(string.Format(await _localizationService.GetResourceAsync("Plugins.Misc.RestApi.Sessions.RevokedCount"), count));

        return RedirectToAction(nameof(EditClient), new { id });
    }

    #endregion
}
