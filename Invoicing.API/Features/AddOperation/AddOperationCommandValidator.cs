using FluentValidation;
using Invoicing.Domain.Enums;

namespace Invoicing.API.Features.AddOperation;

public class AddOperationCommandValidator : AbstractValidator<AddOperationCommand>
{
    public AddOperationCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(100_000);

        RuleFor(x => x.PricePerDay)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(10_000)
            .PrecisionScale(7, 2, true);

        RuleFor(x => x.PricePerDay)
            .NotNull()
            .When(x => x.Type == OperationType.StartService)
            .WithMessage("Price per day must not be null for starting the service.");

        RuleFor(x => x.PricePerDay)
            .Null()
            .When(x => x.Type != OperationType.StartService)
            .WithMessage("Price per day must be null for operations other than starting the service.");
    }
}