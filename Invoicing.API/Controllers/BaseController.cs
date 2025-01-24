using Invoicing.API.Dto.Result;
using Microsoft.AspNetCore.Mvc;

namespace Invoicing.API.Controllers;

[ApiController]
[Produces("application/json")]
[Consumes("application/json")]
public abstract class BaseController : ControllerBase
{
    protected ActionResult<TValue> CreateResponse<TValue>(HttpResult<TValue> result)
    {
        if (result.Pagination != null)
        {
            Response.Headers.Append(
                "X-Pagination",
                $"page={result.Pagination.Page}," +
                $"pageSize={result.Pagination.PageSize}," +
                $"totalCount={result.Pagination.TotalCount}," +
                $"totalPages={result.Pagination.TotalPages}"
            );
            Response.Headers.Append("Access-Control-Expose-Headers", "X-Pagination");
        }

        return result.StatusCode switch
        {
            204 => StatusCode(result.StatusCode),
            >= 200 and < 300 => StatusCode(result.StatusCode, result.Value),
            _ when result.IsError => StatusCode(result.StatusCode, new ErrorResult(result.Error)),
            _ when result.HasValidationErrors =>
                StatusCode(result.StatusCode, new { Errors = result.ValidationErrors }),
            _ => throw new Exception("Failed to create response")
        };
    }
}