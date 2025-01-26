using FluentValidation;
using Invoicing.API.Dto.Result;
using MediatR;

namespace Invoicing.API.Mediator;

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
        var context = new ValidationContext<TRequest>(request);

        var validationFailures = await Task.WhenAll(
            _validators.Select(validator => validator.ValidateAsync(context, cancellationToken))
        );

        var errors = validationFailures
            .Where(validationResult => !validationResult.IsValid)
            .SelectMany(validationResult => validationResult.Errors)
            .ToList();

        if (errors.Count != 0)
        {
            if (!typeof(HttpResult<>).IsAssignableFrom(typeof(TResponse).GetGenericTypeDefinition()))
                throw new ValidationException(errors);

            var result = (TResponse?)Activator.CreateInstance(typeof(TResponse), errors);
            return result ?? throw new InvalidOperationException("Cannot create HttpResult in ValidationBehavior");
        }

        var response = await next();

        return response;
    }
}