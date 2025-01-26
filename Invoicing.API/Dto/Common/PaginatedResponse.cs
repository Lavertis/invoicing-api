namespace Invoicing.API.Dto.Common;

public class PaginatedResponse<T>
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public IEnumerable<T> Records { get; init; } = [];
}