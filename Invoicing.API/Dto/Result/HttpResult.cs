using FluentValidation.Results;

namespace Invoicing.API.Dto.Result;

public sealed class HttpResult<TValue> : Result<HttpResult<TValue>, TValue>
{
    public HttpResult()
    {
    }

    public HttpResult(IEnumerable<ValidationFailure> validationFailures)
    {
        SetValidationErrors(validationFailures);
    }

    public IDictionary<string, string[]>? ValidationErrors { get; private set; }
    public int StatusCode { get; private set; } = StatusCodes.Status200OK;
    public bool HasValidationErrors => ValidationErrors != null;

    public HttpResult<TValue> WithStatusCode(int statusCode)
    {
        StatusCode = statusCode;
        return this;
    }

    private void SetValidationErrors(IEnumerable<ValidationFailure> validationFailures)
    {
        ValidationErrors = validationFailures
            .GroupBy(x => x.PropertyName, s => s.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
        StatusCode = StatusCodes.Status400BadRequest;
    }
}