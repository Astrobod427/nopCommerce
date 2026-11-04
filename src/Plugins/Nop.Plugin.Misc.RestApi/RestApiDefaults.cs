namespace Nop.Plugin.Misc.RestApi;

/// <summary>
/// Represents plugin constants
/// </summary>
public static class RestApiDefaults
{
    /// <summary>
    /// Gets the plugin system name
    /// </summary>
    public static string SystemName => "Misc.RestApi";

    /// <summary>
    /// Gets the base path of every API route
    /// </summary>
    public const string ROUTE_PREFIX = "api/v1";

    /// <summary>
    /// Gets the configuration route name
    /// </summary>
    public static string ConfigurationRouteName => "Plugin.Misc.RestApi.Configure";

    /// <summary>
    /// Gets the system name of the admin menu item
    /// </summary>
    public static string AdminMenuSystemName => "Misc.RestApi";

    /// <summary>
    /// Gets the HTTP context item key holding the authenticated API token
    /// </summary>
    public static string HttpContextTokenKey => "Nop.Plugin.Misc.RestApi.Token";

    /// <summary>
    /// Gets the HTTP context item key holding the API client of a back-office token
    /// </summary>
    public static string HttpContextClientKey => "Nop.Plugin.Misc.RestApi.Client";

    /// <summary>
    /// Gets the name of the scheduled task that purges expired tokens
    /// </summary>
    public static string PurgeTokensTaskName => "REST API: purge expired sessions";

    /// <summary>
    /// Gets the type of the scheduled task that purges expired tokens
    /// </summary>
    public static string PurgeTokensTaskType => "Nop.Plugin.Misc.RestApi.Services.PurgeExpiredTokensTask";

    /// <summary>
    /// Gets the activity log type for API sign-in failures
    /// </summary>
    public static string ActivityLogSignInFailed => "RestApi.SignInFailed";

    /// <summary>
    /// Gets the activity log type for back-office writes made through the API
    /// </summary>
    public static string ActivityLogBackOfficeWrite => "RestApi.BackOfficeWrite";

    /// <summary>
    /// Gets the activity log type for requests refused by the API protections (rate limit, IP restriction)
    /// </summary>
    public static string ActivityLogRequestRefused => "RestApi.RequestRefused";

    /// <summary>
    /// Gets the lifetime of the pending session issued while the second factor is awaited
    /// </summary>
    public static TimeSpan MfaPendingTokenLifetime => TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets the number of wrong second-factor codes after which the pending session is destroyed
    /// </summary>
    public static int MfaMaxAttempts => 5;

    /// <summary>
    /// Gets the system name of the Google Authenticator multi-factor authentication plugin
    /// </summary>
    public static string GoogleAuthenticatorSystemName => "MultiFactorAuth.GoogleAuthenticator";

    /// <summary>
    /// Gets the request headers a browser may send to the API (CORS)
    /// </summary>
    public static string CorsAllowedHeaders => "Authorization, Content-Type, Accept, X-Language-Id, X-Currency-Id, Idempotency-Key";

    /// <summary>
    /// Gets the response headers a browser may read (CORS)
    /// </summary>
    public static string CorsExposedHeaders => "Retry-After, WWW-Authenticate";
}
