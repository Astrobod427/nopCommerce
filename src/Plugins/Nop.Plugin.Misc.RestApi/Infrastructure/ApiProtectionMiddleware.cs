using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Data;
using Nop.Services.Helpers;
using Nop.Plugin.Misc.RestApi.Api;
using Nop.Plugin.Misc.RestApi.Services;

namespace Nop.Plugin.Misc.RestApi.Infrastructure;

/// <summary>
/// First line of the API: security headers, browser access from other web origins (CORS) and per IP request limits.
/// Runs before the authentication middleware so that refused requests cost no database access.
/// </summary>
public class ApiProtectionMiddleware
{
    protected static readonly string[] _authPaths = ["auth/login", "auth/register", "auth/guest", "auth/token", "auth/mfa"];

    protected readonly RequestDelegate _next;

    public ApiProtectionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Gets the allowed origin to send back for the request origin, or null when the origin is not allowed
    /// </summary>
    protected static string GetAllowedOrigin(RestApiSettings settings, string origin)
    {
        if (string.IsNullOrEmpty(origin) || string.IsNullOrWhiteSpace(settings.CorsAllowedOrigins))
            return null;

        foreach (var allowed in settings.CorsAllowedOrigins.Split(['\n', '\r', ',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (allowed == "*")
                return "*";

            if (string.Equals(allowed.TrimEnd('/'), origin, StringComparison.OrdinalIgnoreCase))
                return origin;
        }

        return null;
    }

    protected static bool IsAuthRequest(HttpRequest request)
    {
        if (!HttpMethods.IsPost(request.Method))
            return false;

        var path = request.Path.Value?.Trim('/') ?? string.Empty;
        var prefix = RestApiDefaults.ROUTE_PREFIX + "/";

        return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && _authPaths.Any(authPath => path[prefix.Length..].Equals(authPath, StringComparison.OrdinalIgnoreCase));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/" + RestApiDefaults.ROUTE_PREFIX, StringComparison.OrdinalIgnoreCase)
            || !DataSettingsManager.IsDatabaseInstalled())
        {
            await _next(context);
            return;
        }

        var services = context.RequestServices;
        var settings = services.GetRequiredService<RestApiSettings>();
        var headers = context.Response.Headers;

        //responses hold personal data: never cached, never sniffed
        headers.XContentTypeOptions = "nosniff";
        headers.CacheControl = "no-store";
        headers.Pragma = "no-cache";
        headers["Referrer-Policy"] = "no-referrer";

        //browser access from other web origins; bearer tokens only, so credentials are never allowed
        var origin = context.Request.Headers.Origin.ToString();
        var allowedOrigin = GetAllowedOrigin(settings, origin);
        if (!string.IsNullOrEmpty(origin))
            headers.Vary = "Origin";
        if (allowedOrigin != null)
        {
            headers.AccessControlAllowOrigin = allowedOrigin;
            headers.AccessControlExposeHeaders = RestApiDefaults.CorsExposedHeaders;
        }

        if (HttpMethods.IsOptions(context.Request.Method) && context.Request.Headers.ContainsKey("Access-Control-Request-Method"))
        {
            if (allowedOrigin != null)
            {
                headers.AccessControlAllowMethods = "GET, POST, PUT, DELETE";
                headers.AccessControlAllowHeaders = RestApiDefaults.CorsAllowedHeaders;
                headers.AccessControlMaxAge = "600";
            }

            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        var ip = services.GetRequiredService<IWebHelper>().GetCurrentIpAddress();
        var (allowed, retryAfter) = services.GetRequiredService<ApiRateLimiter>().TryAcquire(settings, ip, IsAuthRequest(context.Request));
        if (!allowed)
        {
            var seconds = Math.Max(1, (int)Math.Ceiling(retryAfter?.TotalSeconds ?? 10));
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json; charset=utf-8";
            headers.RetryAfter = seconds.ToString();
            await context.Response.WriteAsync(JsonConvert.SerializeObject(
                new ApiErrorResponse("rate_limited", $"Too many requests. Retry in {seconds} s."), ApiJson.Settings));
            return;
        }

        await _next(context);
    }
}
