using Nop.Core.Domain.Customers;
using Nop.Services.Security;

namespace Nop.Plugin.Misc.RestApi.Services;

/// <summary>
/// Represents the plugin permissions; they appear in Configuration > Access control list
/// </summary>
public class RestApiPermissionConfigManager : IPermissionConfigManager
{
    /// <summary>
    /// Manage the plugin: settings, API clients, active sessions
    /// </summary>
    public const string MANAGE_API = "Misc.RestApi.Admin.Manage";

    /// <summary>
    /// Use the storefront API (sign in from an app, browse, cart, checkout)
    /// </summary>
    public const string USE_STOREFRONT_API = "Misc.RestApi.PublicStore.UseStorefrontApi";

    /// <summary>
    /// Use the back-office API (granted to the customer accounts of API clients)
    /// </summary>
    public const string USE_BACKOFFICE_API = "Misc.RestApi.Admin.UseBackOfficeApi";

    /// <summary>
    /// Gets all permission configurations
    /// </summary>
    public IList<PermissionConfig> AllConfigs =>
    [
        new("Admin area. REST API. Manage settings, API clients and sessions", MANAGE_API, nameof(StandardPermission.Configuration), NopCustomerDefaults.AdministratorsRoleName),
        new("Admin area. REST API. Use the back-office API", USE_BACKOFFICE_API, nameof(StandardPermission.Configuration), NopCustomerDefaults.AdministratorsRoleName),
        new("Public store. REST API. Use the storefront API", USE_STOREFRONT_API, nameof(StandardPermission.PublicStore), NopCustomerDefaults.AdministratorsRoleName, NopCustomerDefaults.RegisteredRoleName, NopCustomerDefaults.GuestsRoleName)
    ];
}
