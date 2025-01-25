using FluentValidation;

namespace Invoicing.API.Features.Invoices.CreateInvoices;

public sealed class CreateInvoicesCommandValidator : AbstractValidator<CreateInvoicesCommand>
{
    public CreateInvoicesCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Month)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(12);

        RuleFor(x => x.Year)
            .GreaterThanOrEqualTo(2000);
    }
}