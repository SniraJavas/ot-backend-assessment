
namespace OT.Assessment.App.Models;

public sealed class PagedResponse<T>
{
    public IReadOnlyList<T> Data { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
    public int TotalPages { get; init; }
}
