using Invoicing.API.Dto.Common;
using Invoicing.API.Dto.Result;
using Invoicing.API.Features.Invoices.Shared;
using Invoicing.Domain.Entities;
using Invoicing.Infrastructure.Database;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicing.API.Features.Invoices.GetInvoices;

public sealed class GetInvoicesQueryHandler(InvoicingDbContext context)
    : IRequestHandler<GetInvoicesQuery, HttpResult<PaginatedResponse<InvoiceResponse>>>
{
    public async Task<HttpResult<PaginatedResponse<InvoiceResponse>>> Handle(
        GetInvoicesQuery request,
        CancellationToken cancellationToken)
    {
        var result = new HttpResult<PaginatedResponse<InvoiceResponse>>();

        var query = context.Invoices
            .Include(i => i.Items.OrderBy(item => item.StartDate).ThenBy(item => item.EndDate))
            .AsNoTrackingWithIdentityResolution();

        query = ApplyFiltering(request, query);
        var response = await CreateResponse(request, query, cancellationToken);
        return result.WithValue(response);
    }

    private async Task<PaginatedResponse<InvoiceResponse>> CreateResponse(
        GetInvoicesQuery request,
        IQueryable<Invoice> query,
        CancellationToken cancellationToken)
    {
        var paginatedQuery = Paginate(request, query);
        var invoiceResponses = await paginatedQuery
            .ProjectToType<InvoiceResponse>()
            .ToListAsync(cancellationToken: cancellationToken);

        return new PaginatedResponse<InvoiceResponse>
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = await query.CountAsync(cancellationToken),
            Records = invoiceResponses
        };
    }

    private static IQueryable<Invoice> Paginate(GetInvoicesQuery request, IQueryable<Invoice> invoiceQuery)
    {
        return invoiceQuery
            .OrderBy(i => i.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize);
    }

    private static IQueryable<Invoice> ApplyFiltering(GetInvoicesQuery request, IQueryable<Invoice> query)
    {
        if (request.ClientId is not null)
            query = query.Where(i => i.ClientId == request.ClientId);
        if (request.Month is not null)
            query = query.Where(i => i.Month == request.Month);
        if (request.Year is not null)
            query = query.Where(i => i.Year == request.Year);
        return query;
    }
}