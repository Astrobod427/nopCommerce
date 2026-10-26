using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Data;
using Nop.Plugin.Misc.RestApi.Api;
using Nop.Plugin.Misc.RestApi.Services;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Themes;

namespace Nop.Plugin.Misc.RestApi.Infrastructure;

/// <summary>
/// Resolves the bearer token of API requests and makes its customer the current customer of the request,
/// so that every nopCommerce service (prices, taxes, discounts, ACL, cart) works as for the web store.
/// Runs before the nopCommerce middlewares that would otherwise create a guest customer per request.
/// </summary>
public class ApiAuthenticationMiddleware
{
    protected readonly RequestDelegate _next;

    public ApiAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    protected static async Task WriteErrorAsync(HttpContext context, int statusCode, string code, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        if (statusCode == StatusCodes.Status401Unauthorized)
            context.Response.Headers.WWWAuthenticate = "Bearer";
        await context.Response.WriteAsync(JsonConvert.SerializeObject(new ApiErrorResponse(code, message), ApiJson.Settings));
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
        if (!settings.StorefrontEnabled && !settings.BackOfficeEnabled)
        {
            await WriteErrorAsync(context, StatusCodes.Status404NotFound, "api_disabled", "The API is disabled.");
            return;
        }

        //the API is stateless: no theme resolution (it would create a guest customer) and no cookies
        context.Items[NopThemeDefaults.HttpContextThemeCacheKey] = string.Empty;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.Remove("Set-Cookie");
            return Task.CompletedTask;
        });

        var authorization = context.Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var tokenService = services.GetRequiredService<ApiTokenService>();
            var token = await tokenService.GetValidTokenAsync(authorization["Bearer ".Length..].Trim());
            var store = await services.GetRequiredService<IStoreContext>().GetCurrentStoreAsync();
            var customer = token == null ? null : await services.GetRequiredService<ICustomerService>().GetCustomerByIdAsync(token.CustomerId);

            if (token == null || token.StoreId != store.Id || customer == null || customer.Deleted || !customer.Active || customer.RequireReLogin)
            {
                await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, "invalid_token", "The access token is missing, invalid or expired.");
                return;
            }

            var workContext = services.GetRequiredService<IWorkContext>();
            await workContext.SetCurrentCustomerAsync(customer);
            context.Items[RestApiDefaults.HttpContextTokenKey] = token;

            //optional working language and currency
            if (int.TryParse(context.Request.Headers["X-Language-Id"], out var languageId) && languageId > 0)
            {
                var language = await services.GetRequiredService<ILanguageService>().GetLanguageByIdAsync(languageId);
                if (language is { Published: true })
                    await workContext.SetWorkingLanguageAsync(language);
            }

            if (int.TryParse(context.Request.Headers["X-Currency-Id"], out var currencyId) && currencyId > 0)
            {
                var currency = await services.GetRequiredService<ICurrencyService>().GetCurrencyByIdAsync(currencyId);
                if (currency is { Published: true })
                    await workContext.SetWorkingCurrencyAsync(currency);
            }
        }

        await _next(context);
    }
}
