using Nop.Core;

namespace Nop.Plugin.Misc.RestApi.Domain;

/// <summary>
/// Represents an issued access token (a signed-in API session)
/// </summary>
public class ApiToken : BaseEntity
{
    /// <summary>
    /// Gets or sets the SHA-256 hash of the token (the token itself is never stored)
    /// </summary>
    public string TokenHash { get; set; }

    /// <summary>
    /// Gets or sets the token kind identifier
    /// </summary>
    public int KindId { get; set; }

    /// <summary>
    /// Gets or sets the customer identifier
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the API client identifier (back-office tokens only)
    /// </summary>
    public int? ApiClientId { get; set; }

    /// <summary>
    /// Gets or sets the store identifier the token was issued for
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the IP address the token was issued to
    /// </summary>
    public string IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the date and time of creation
    /// </summary>
    public DateTime CreatedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the date and time of expiration
    /// </summary>
    public DateTime ExpiresOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the date and time of the last use
    /// </summary>
    public DateTime? LastUsedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the token kind
    /// </summary>
    public ApiTokenKind Kind
    {
        get => (ApiTokenKind)KindId;
        set => KindId = (int)value;
    }
}
