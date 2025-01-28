using Invoicing.API.Dto.Result;

namespace Invoicing.API.Extensions;

public static class ResultExtensions
{
    public static IResult CreateResponse<TValue>(this HttpResult<TValue> result)
    {
        return result.StatusCode switch
        {
            204 => Results.StatusCode(result.StatusCode),
            >= 200 and < 300 => Results.Json(result.Data, statusCode: result.StatusCode),
            _ => HandleErrorResponse(result)
        };
    }

    private static IResult HandleErrorResponse<TValue>(HttpResult<TValue> result)
    {
        if (result.HasValidationErrors)
            return Results.Json(new { Errors = result.ValidationErrors }, statusCode: result.StatusCode);

        if (result.StatusCode is >= 400 and < 600)
        {
            return Results.Json(
                result.ErrorMessage != null ? new ErrorResult(result.ErrorMessage) : null,
                statusCode: result.StatusCode
            );
        }

        return Results.Json(new ErrorResult("An error occurred while processing your request"), statusCode: 500);
    }
}