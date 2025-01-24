using FluentValidation.Results;

namespace Invoicing.API.Dto.Result;

public class HttpResult<TValue> : Result<HttpResult<TValue>, TValue>
{
    public IDictionary<string, string[]>? ValidationErrors { get; private set; }
    public int StatusCode { get; private set; } = StatusCodes.Status200OK;
    public bool HasValidationErrors => ValidationErrors != null;
    public PaginationMetadata? Pagination { get; private set; }

    public HttpResult<TValue> WithStatusCode(int statusCode)
    {
        StatusCode = statusCode;
        return this;
    }

    public HttpResult<TValue> WithValidationErrors(IEnumerable<ValidationFailure> validationFailures)
    {
        ValidationErrors = validationFailures
            .GroupBy(x => x.PropertyName, s => s.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
        StatusCode = StatusCodes.Status400BadRequest;
        return this;
    }

    public HttpResult<TValue> WithValidationErrors(IDictionary<string, string[]>? validationErrors)
    {
        ValidationErrors = validationErrors;
        StatusCode = StatusCodes.Status400BadRequest;
        return this;
    }

    public HttpResult<TValue> WithPagination(int pageNumber, int pageSize, int totalRecords)
    {
        Pagination = new PaginationMetadata
        {
            Page = pageNumber,
            PageSize = pageSize,
            TotalCount = totalRecords
        };
        return this;
    }
}

public class PaginationMetadata
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}