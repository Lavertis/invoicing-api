using FluentValidation;

namespace Invoicing.API.Features.CalculateInvoices;

public class CalculateInvoicesCommandValidator : AbstractValidator<CalculateInvoicesCommand>
{
    public CalculateInvoicesCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Month)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(12);

        RuleFor(x => x.Year)
            .GreaterThanOrEqualTo(2000);
    }
}