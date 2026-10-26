namespace Nop.Plugin.Misc.RestApi.Domain;

/// <summary>
/// Represents the kind of an API access token
/// </summary>
public enum ApiTokenKind
{
    /// <summary>
    /// Storefront token: a shopper (guest or registered customer) using an app
    /// </summary>
    Storefront = 10,

    /// <summary>
    /// Back-office token: an API client acting for its customer account
    /// </summary>
    BackOffice = 20
}
