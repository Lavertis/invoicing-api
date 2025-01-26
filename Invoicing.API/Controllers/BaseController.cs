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
        return result.StatusCode switch
        {
            204 => StatusCode(result.StatusCode),
            >= 200 and < 300 => StatusCode(result.StatusCode, result.Value),
            _ => HandleErrorResponse(result)
        };
    }

    private ActionResult<TValue> HandleErrorResponse<TValue>(HttpResult<TValue> result)
    {
        if (result.HasValidationErrors)
            return StatusCode(result.StatusCode, new { Errors = result.ValidationErrors });

        if (result.StatusCode is >= 400 and <= 600)
        {
            return StatusCode(
                result.StatusCode,
                result.ErrorMessage != null ? new ErrorResult(result.ErrorMessage) : null
            );
        }

        return StatusCode(500, new ErrorResult("An error occurred while processing your request"));
    }
}