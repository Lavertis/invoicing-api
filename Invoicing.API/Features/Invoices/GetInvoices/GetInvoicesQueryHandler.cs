using AutoMapper;
using FluentValidation;
using Invoicing.API.Dto.Result;
using Invoicing.API.Features.Invoices.Shared;
using Invoicing.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicing.API.Features.Invoices.GetInvoices;

public sealed class GetInvoicesQueryHandler(
    InvoicingDbContext context,
    IValidator<GetInvoicesQuery> validator,
    IMapper mapper
) : IRequestHandler<GetInvoicesQuery, HttpResult<IEnumerable<InvoiceResponse>>>
{
    public async Task<HttpResult<IEnumerable<InvoiceResponse>>> Handle(
        GetInvoicesQuery request,
        CancellationToken cancellationToken)
    {
        var result = new HttpResult<IEnumerable<InvoiceResponse>>();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return result.WithValidationErrors(validationResult.Errors);

        var invoiceQuery = context.Invoices
            .Include(i => i.Items.OrderBy(item => item.StartDate).ThenBy(item => item.EndDate))
            .AsNoTrackingWithIdentityResolution();

        if (request.ClientId is not null)
            invoiceQuery = invoiceQuery.Where(i => i.ClientId == request.ClientId);
        if (request.Month is not null)
            invoiceQuery = invoiceQuery.Where(i => i.Month == request.Month);
        if (request.Year is not null)
            invoiceQuery = invoiceQuery.Where(i => i.Year == request.Year);

        var invoices = await invoiceQuery.ToListAsync(cancellationToken);
        var response = invoices.Select(mapper.Map<InvoiceResponse>);
        return result.WithValue(response);
    }
}