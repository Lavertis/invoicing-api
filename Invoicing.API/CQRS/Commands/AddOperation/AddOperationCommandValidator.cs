using FluentValidation;

namespace Invoicing.API.CQRS.Commands.AddOperation;

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
    }
}