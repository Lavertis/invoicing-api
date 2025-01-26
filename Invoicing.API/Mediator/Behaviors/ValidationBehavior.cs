using FluentValidation;
using FluentValidation.Results;
using Invoicing.API.Dto.Result;
using MediatR;

namespace Invoicing.API.Mediator.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var errors = await ValidateRequestAsync(request, cancellationToken);
        if (errors.Count != 0) return HandleValidationErrors(errors);
        return await next();
    }

    private async Task<ICollection<ValidationFailure>> ValidateRequestAsync(TRequest request,
        CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var validationFailures = await Task.WhenAll(
            _validators.Select(validator => validator.ValidateAsync(context, cancellationToken))
        );
        return validationFailures
            .Where(validationResult => !validationResult.IsValid)
            .SelectMany(validationResult => validationResult.Errors)
            .ToList();
    }

    private static TResponse HandleValidationErrors(IEnumerable<ValidationFailure> errors)
    {
        if (!typeof(HttpResult<>).IsAssignableFrom(typeof(TResponse).GetGenericTypeDefinition()))
            throw new ValidationException(errors);

        var result = (TResponse?)Activator.CreateInstance(typeof(TResponse), errors);
        return result ?? throw new InvalidOperationException("Cannot create HttpResult in ValidationBehavior");
    }
}