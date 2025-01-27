using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Invoicing.API.Dto.Common;

public class PaginationQuery
{
    private readonly int _page;
    private readonly int _pageSize;

    [DefaultValue(1)]
    [Range(1, int.MaxValue)]
    public int Page
    {
        get => _page;
        init => _page = value > 0 ? value : 1;
    }

    [DefaultValue(10)]
    [Range(1, 50)]
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value is > 0 and <= 50 ? value : 10;
    }
}