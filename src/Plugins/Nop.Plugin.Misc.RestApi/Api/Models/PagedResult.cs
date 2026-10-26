namespace Nop.Plugin.Misc.RestApi.Api.Models;

/// <summary>
/// Represents a page of results
/// </summary>
public record PagedResult<T>
{
    public IList<T> Items { get; init; } = [];

    public int PageIndex { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages { get; init; }
}
