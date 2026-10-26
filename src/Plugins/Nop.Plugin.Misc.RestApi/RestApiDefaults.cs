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
}
