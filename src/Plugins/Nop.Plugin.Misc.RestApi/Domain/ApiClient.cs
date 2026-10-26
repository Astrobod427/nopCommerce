using Nop.Core;

namespace Nop.Plugin.Misc.RestApi.Domain;

/// <summary>
/// Represents a back-office API client (third-party software). The client acts on behalf of a nopCommerce
/// customer account, so its rights are those of the account's customer roles (ACL).
/// </summary>
public class ApiClient : BaseEntity
{
    /// <summary>
    /// Gets or sets the display name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the public client identifier
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    /// Gets or sets the SHA-256 hash of the client secret (the secret itself is never stored)
    /// </summary>
    public string SecretHash { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the customer account the client acts for
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the client is active
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// Gets or sets the date and time of creation
    /// </summary>
    public DateTime CreatedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the date and time of the last successful authentication
    /// </summary>
    public DateTime? LastUsedOnUtc { get; set; }
}
