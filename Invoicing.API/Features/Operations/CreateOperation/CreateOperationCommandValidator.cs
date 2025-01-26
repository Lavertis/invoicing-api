using FluentValidation;
using Invoicing.Domain.Enums;

namespace Invoicing.API.Features.Operations.CreateOperation;

public sealed class CreateOperationCommandValidator : AbstractValidator<CreateOperationCommand>
{
    public CreateOperationCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(100_000);

        RuleFor(x => x.Quantity)
            .NotNull()
            .When(x => x.Type == OperationType.StartService)
            .WithMessage("Quantity must be provided for starting the service.");

        RuleFor(x => x.Quantity)
            .Null()
            .When(x => x.Type != OperationType.StartService)
            .WithMessage("Quantity cannot be provided for operations other than starting the service.");

        RuleFor(x => x.PricePerDay)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(10_000)
            .PrecisionScale(7, 2, true);

        RuleFor(x => x.PricePerDay)
            .NotNull()
            .When(x => x.Type == OperationType.StartService)
            .WithMessage("Price per day must be provided for starting the service.");

        RuleFor(x => x.PricePerDay)
            .Null()
            .When(x => x.Type != OperationType.StartService)
            .WithMessage("Price per day must cannot be provided for operations other than starting the service.");
    }
}