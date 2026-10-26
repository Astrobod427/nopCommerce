using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Misc.RestApi.Api.Models;
using Nop.Plugin.Misc.RestApi.Domain;

namespace Nop.Plugin.Misc.RestApi.Api.Controllers;

/// <summary>
/// Base class of API controllers (plain JSON, no MVC views, no anti-forgery: authentication is by bearer token)
/// </summary>
[ApiController]
[IgnoreAntiforgeryToken]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Gets the access token of the request (null for anonymous endpoints)
    /// </summary>
    protected ApiToken CurrentToken => HttpContext.Items.TryGetValue(RestApiDefaults.HttpContextTokenKey, out var value) ? value as ApiToken : null;

    protected IActionResult ApiOk(object value)
    {
        return new JsonResult(value, ApiJson.Settings) { StatusCode = StatusCodes.Status200OK };
    }

    protected IActionResult ApiCreated(object value)
    {
        return new JsonResult(value, ApiJson.Settings) { StatusCode = StatusCodes.Status201Created };
    }

    protected IActionResult ApiError(int statusCode, string code, string message, IEnumerable<string> details = null)
    {
        return new JsonResult(new ApiErrorResponse(code, message, details), ApiJson.Settings) { StatusCode = statusCode };
    }

    protected IActionResult ApiNotFound(string what = "Resource")
    {
        return ApiError(StatusCodes.Status404NotFound, "not_found", $"{what} not found.");
    }

    protected IActionResult ApiBadRequest(string message, IEnumerable<string> details = null)
    {
        return ApiError(StatusCodes.Status400BadRequest, "bad_request", message, details);
    }

    protected static PagedResult<TDto> ToPaged<TEntity, TDto>(IPagedList<TEntity> list, IList<TDto> items)
    {
        return new PagedResult<TDto>
        {
            Items = items,
            PageIndex = list.PageIndex,
            PageSize = list.PageSize,
            TotalCount = list.TotalCount,
            TotalPages = list.TotalPages
        };
    }

    protected static int ClampPageSize(int pageSize, RestApiSettings settings)
    {
        var max = settings.MaxPageSize > 0 ? settings.MaxPageSize : 100;
        return pageSize <= 0 ? Math.Min(20, max) : Math.Min(pageSize, max);
    }
}
