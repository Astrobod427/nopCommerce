using System.ComponentModel.DataAnnotations;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.RestApi.Models;

/// <summary>
/// Represents the plugin settings page model
/// </summary>
public record ConfigurationModel : BaseNopModel
{
    [NopResourceDisplayName("Plugins.Misc.RestApi.Fields.StorefrontEnabled")]
    public bool StorefrontEnabled { get; set; }

    [NopResourceDisplayName("Plugins.Misc.RestApi.Fields.BackOfficeEnabled")]
    public bool BackOfficeEnabled { get; set; }

    [NopResourceDisplayName("Plugins.Misc.RestApi.Fields.StorefrontTokenLifetimeDays")]
    public int StorefrontTokenLifetimeDays { get; set; }

    [NopResourceDisplayName("Plugins.Misc.RestApi.Fields.BackOfficeTokenLifetimeMinutes")]
    public int BackOfficeTokenLifetimeMinutes { get; set; }

    [NopResourceDisplayName("Plugins.Misc.RestApi.Fields.MaxPageSize")]
    public int MaxPageSize { get; set; }

    [NopResourceDisplayName("Plugins.Misc.RestApi.Fields.AppPrimaryColor")]
    public string AppPrimaryColor { get; set; }

    [NopResourceDisplayName("Plugins.Misc.RestApi.Fields.AppSecondaryColor")]
    public string AppSecondaryColor { get; set; }

    [UIHint("Picture")]
    [NopResourceDisplayName("Plugins.Misc.RestApi.Fields.AppLogoPictureId")]
    public int AppLogoPictureId { get; set; }

    public string ApiBaseUrl { get; set; }

    public int ActiveStorefrontSessions { get; set; }

    public int ActiveBackOfficeSessions { get; set; }
}

/// <summary>
/// Represents an API client in the list
/// </summary>
public record ApiClientListItemModel
{
    public int Id { get; init; }

    public string Name { get; init; }

    public string ClientId { get; init; }

    public string CustomerEmail { get; init; }

    public bool CustomerHasPermission { get; init; }

    public bool Active { get; init; }

    public int ActiveSessions { get; init; }

    public DateTime CreatedOn { get; init; }

    public DateTime? LastUsedOn { get; init; }
}

/// <summary>
/// Represents the API client list page model
/// </summary>
public record ApiClientListModel : BaseNopModel
{
    public IList<ApiClientListItemModel> Clients { get; init; } = [];
}

/// <summary>
/// Represents the API client creation and edition model
/// </summary>
public record ApiClientModel : BaseNopEntityModel
{
    [NopResourceDisplayName("Plugins.Misc.RestApi.Clients.Fields.Name")]
    public string Name { get; set; }

    [NopResourceDisplayName("Plugins.Misc.RestApi.Clients.Fields.CustomerEmail")]
    public string CustomerEmail { get; set; }

    [NopResourceDisplayName("Plugins.Misc.RestApi.Clients.Fields.Active")]
    public bool Active { get; set; }

    [NopResourceDisplayName("Plugins.Misc.RestApi.Clients.Fields.ClientId")]
    public string ClientId { get; set; }

    public int CustomerId { get; set; }

    public bool CustomerHasPermission { get; set; }

    public int ActiveSessions { get; set; }

    /// <summary>
    /// Clear secret, shown only right after creation or regeneration
    /// </summary>
    public string NewSecret { get; set; }
}

/// <summary>
/// Represents an active session in the list
/// </summary>
public record ApiSessionModel
{
    public int Id { get; init; }

    public string Kind { get; init; }

    public int CustomerId { get; init; }

    public string CustomerEmail { get; init; }

    public string ClientName { get; init; }

    public string IpAddress { get; init; }

    public DateTime CreatedOn { get; init; }

    public DateTime? LastUsedOn { get; init; }

    public DateTime ExpiresOn { get; init; }
}

/// <summary>
/// Represents the active session list page model
/// </summary>
public record ApiSessionListModel : BaseNopModel
{
    public IList<ApiSessionModel> Sessions { get; init; } = [];

    public int TotalCount { get; init; }
}
