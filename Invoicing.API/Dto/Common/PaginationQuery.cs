namespace Invoicing.API.Dto.Common;

public class PaginationQuery
{
    private readonly int _page = 1;
    private readonly int _pageSize = 10;

    public int Page
    {
        get => _page;
        init => _page = value > 0 ? value : 1;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value is > 0 and <= 50 ? value : 10;
    }
}