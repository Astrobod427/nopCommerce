using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core;
using Nop.Plugin.Misc.RestApi.Domain;
using Nop.Plugin.Misc.RestApi.Services;
using Nop.Services.Security;

namespace Nop.Plugin.Misc.RestApi.Api.Filters;

/// <summary>
/// Requires an access token of the given kind and, optionally, nopCommerce permissions
/// (Configuration > Access control list) for the token's customer
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class ApiAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    public ApiAuthorizeAttribute(ApiTokenKind kind, params string[] permissions)
    {
        Kind = kind;
        Permissions = permissions;
    }

    public ApiTokenKind Kind { get; }

    public string[] Permissions { get; }

    protected static IActionResult Error(int statusCode, string code, string message)
    {
        return new JsonResult(new ApiErrorResponse(code, message), ApiJson.Settings) { StatusCode = statusCode };
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var services = context.HttpContext.RequestServices;
        var settings = services.GetRequiredService<RestApiSettings>();

        var enabled = Kind == ApiTokenKind.Storefront ? settings.StorefrontEnabled : settings.BackOfficeEnabled;
        if (!enabled)
        {
            context.Result = Error(StatusCodes.Status404NotFound, "api_disabled", "This part of the API is disabled.");
            return;
        }

        if (!context.HttpContext.Items.TryGetValue(RestApiDefaults.HttpContextTokenKey, out var value) || value is not ApiToken token)
        {
            context.Result = Error(StatusCodes.Status401Unauthorized, "unauthorized", "An access token is required.");
            return;
        }

        if (token.Kind != Kind)
        {
            context.Result = Error(StatusCodes.Status403Forbidden, "wrong_token_kind",
                Kind == ApiTokenKind.Storefront ? "A storefront token is required." : "A back-office token is required.");
            return;
        }

        var workContext = services.GetRequiredService<IWorkContext>();
        var customer = await workContext.GetCurrentCustomerAsync();

        //vendor accounts would need every query restricted to their own products: not supported yet
        if (Kind == ApiTokenKind.BackOffice && await workContext.GetCurrentVendorAsync() != null)
        {
            context.Result = Error(StatusCodes.Status403Forbidden, "vendor_not_supported", "Vendor accounts cannot use the back-office API.");
            return;
        }

        var permissionService = services.GetRequiredService<IPermissionService>();

        var required = new List<string>
        {
            Kind == ApiTokenKind.Storefront ? RestApiPermissionConfigManager.USE_STOREFRONT_API : RestApiPermissionConfigManager.USE_BACKOFFICE_API
        };
        required.AddRange(Permissions);

        foreach (var permission in required)
        {
            if (await permissionService.AuthorizeAsync(permission, customer))
                continue;

            context.Result = Error(StatusCodes.Status403Forbidden, "forbidden", $"Permission '{permission}' is required.");
            return;
        }
    }
}
