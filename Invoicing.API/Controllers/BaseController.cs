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
        if (result.StatusCode == 204)
            return StatusCode(result.StatusCode);
        if (result.StatusCode is >= 200 and < 300)
            return StatusCode(result.StatusCode, result.Value);
        if (result.HasValidationErrors)
            return StatusCode(result.StatusCode, new { Errors = result.ValidationErrors });
        if (result.StatusCode is >= 400 and <= 600)
        {
            return StatusCode(
                result.StatusCode,
                result.ErrorMessage != null ? new ErrorResult(result.ErrorMessage) : null
            );
        }

        throw new Exception("Failed to create response");
    }
}