namespace Nop.Plugin.Misc.RestApi.Api;

/// <summary>
/// Represents an API error body: { "error": { "code", "message", "details" } }
/// </summary>
public record ApiErrorResponse
{
    public ApiErrorResponse(string code, string message, IEnumerable<string> details = null)
    {
        Error = new ApiError(code, message, details?.ToList() ?? []);
    }

    public ApiError Error { get; init; }

    public record ApiError(string Code, string Message, IList<string> Details);
}
