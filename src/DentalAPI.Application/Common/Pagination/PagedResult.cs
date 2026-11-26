namespace DentalAPI.Application.Common.Pagination;

/// <summary>One page of a list together with the total number of items across all pages.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public PagedResult<TResult> Map<TResult>(Func<T, TResult> map) =>
        new(Items.Select(map).ToList(), Page, PageSize, TotalCount);
}
