using FluentValidation;

namespace Invoicing.API.Features.Invoices.GetInvoice;

public sealed class GetInvoiceQueryValidator : AbstractValidator<GetInvoiceQuery>
{
    public GetInvoiceQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Month)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(12);

        RuleFor(x => x.Year)
            .GreaterThanOrEqualTo(2000);
    }
}