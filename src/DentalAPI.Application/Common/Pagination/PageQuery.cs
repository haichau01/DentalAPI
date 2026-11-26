namespace DentalAPI.Application.Common.Pagination;

/// <summary>Page selection for list endpoints, bound from <c>?page=&amp;pageSize=</c>.</summary>
public sealed record PageQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;

    public int Skip => (Page - 1) * PageSize;
}
