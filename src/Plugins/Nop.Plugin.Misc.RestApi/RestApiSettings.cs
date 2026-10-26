using Nop.Core.Configuration;

namespace Nop.Plugin.Misc.RestApi;

/// <summary>
/// Represents plugin settings
/// </summary>
public class RestApiSettings : ISettings
{
    /// <summary>
    /// Gets or sets a value indicating whether the storefront API (apps, shoppers) is enabled
    /// </summary>
    public bool StorefrontEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the back-office API (API clients) is enabled
    /// </summary>
    public bool BackOfficeEnabled { get; set; }

    /// <summary>
    /// Gets or sets the storefront session lifetime in days
    /// </summary>
    public int StorefrontTokenLifetimeDays { get; set; }

    /// <summary>
    /// Gets or sets the back-office session lifetime in minutes
    /// </summary>
    public int BackOfficeTokenLifetimeMinutes { get; set; }

    /// <summary>
    /// Gets or sets the maximum page size of list endpoints
    /// </summary>
    public int MaxPageSize { get; set; }

    /// <summary>
    /// Gets or sets the mobile app primary color
    /// </summary>
    public string AppPrimaryColor { get; set; }

    /// <summary>
    /// Gets or sets the mobile app secondary color
    /// </summary>
    public string AppSecondaryColor { get; set; }

    /// <summary>
    /// Gets or sets the mobile app logo picture identifier (0 = store logo)
    /// </summary>
    public int AppLogoPictureId { get; set; }
}
