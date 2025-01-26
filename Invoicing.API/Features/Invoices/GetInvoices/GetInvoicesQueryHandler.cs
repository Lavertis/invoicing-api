using AutoMapper;
using FluentValidation;
using Invoicing.API.Dto.Common;
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
) : IRequestHandler<GetInvoicesQuery, HttpResult<PaginatedResponse<InvoiceResponse>>>
{
    public async Task<HttpResult<PaginatedResponse<InvoiceResponse>>> Handle(
        GetInvoicesQuery request,
        CancellationToken cancellationToken)
    {
        var result = new HttpResult<PaginatedResponse<InvoiceResponse>>();
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

        var invoiceResponses = await invoiceQuery
            .OrderBy(i => i.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => mapper.Map<InvoiceResponse>(i))
            .ToListAsync(cancellationToken);

        var paginatedResponse = new PaginatedResponse<InvoiceResponse>
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = await invoiceQuery.CountAsync(cancellationToken),
            Records = invoiceResponses
        };
        return result.WithValue(paginatedResponse);
    }
}