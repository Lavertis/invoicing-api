namespace Invoicing.API.Dto.Common;

public record PaginationQuery(int Page = 1, int PageSize = 10)
{
    public int Page { get; } = Page < 1 ? 1 : Page;
    public int PageSize { get; } = PageSize < 0 ? 1 : PageSize > 50 ? 50 : PageSize;
}