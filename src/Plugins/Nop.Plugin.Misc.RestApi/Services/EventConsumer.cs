using Nop.Core.Domain.Customers;
using Nop.Core.Events;
using Nop.Services.Common;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Menu;

namespace Nop.Plugin.Misc.RestApi.Services;

/// <summary>
/// Represents plugin event consumer
/// </summary>
public class EventConsumer : IConsumer<AdminMenuCreatedEvent>,
    IConsumer<EntityUpdatedEvent<Customer>>,
    IConsumer<CustomerPasswordChangedEvent>
{
    #region Fields

    protected readonly ApiTokenService _apiTokenService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IPluginManager<IMiscPlugin> _pluginManager;

    #endregion

    #region Ctor

    public EventConsumer(ApiTokenService apiTokenService,
        ILocalizationService localizationService,
        IPluginManager<IMiscPlugin> pluginManager)
    {
        _apiTokenService = apiTokenService;
        _localizationService = localizationService;
        _pluginManager = pluginManager;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Adds "Configuration > REST API" to the admin menu
    /// </summary>
    public async Task HandleEventAsync(AdminMenuCreatedEvent eventMessage)
    {
        //returns only fully installed plugins (the event may be raised during installation)
        var plugin = await _pluginManager.LoadPluginBySystemNameAsync(RestApiDefaults.SystemName);
        if (plugin == null)
            return;

        eventMessage.RootMenuItem.GetItemBySystemName("Configuration")?.ChildNodes.Add(new AdminMenuItem
        {
            Visible = true,
            SystemName = RestApiDefaults.AdminMenuSystemName,
            Title = await _localizationService.GetResourceAsync("Plugins.Misc.RestApi.Menu"),
            Url = eventMessage.GetMenuItemUrl("RestApiAdmin", "Configure"),
            IconClass = "far fa-dot-circle",
            PermissionNames = [RestApiPermissionConfigManager.MANAGE_API]
        });
    }

    /// <summary>
    /// Revokes the sessions of a customer who is deleted, deactivated or must sign in again
    /// </summary>
    public async Task HandleEventAsync(EntityUpdatedEvent<Customer> eventMessage)
    {
        var customer = eventMessage.Entity;
        if (customer.Deleted || !customer.Active || customer.RequireReLogin)
            await _apiTokenService.RevokeTokensAsync(customerId: customer.Id);
    }

    /// <summary>
    /// Revokes the sessions of a customer whose password changed
    /// </summary>
    public async Task HandleEventAsync(CustomerPasswordChangedEvent eventMessage)
    {
        await _apiTokenService.RevokeTokensAsync(customerId: eventMessage.Password.CustomerId);
    }

    #endregion
}
