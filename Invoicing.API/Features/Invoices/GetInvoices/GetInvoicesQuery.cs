using Invoicing.API.Dto.Common;
using Invoicing.API.Dto.Result;
using Invoicing.API.Features.Invoices.Shared;
using MediatR;

namespace Invoicing.API.Features.Invoices.GetInvoices;

public sealed class GetInvoicesQuery : PaginationQuery, IRequest<HttpResult<PaginatedResponse<InvoiceResponse>>>
{
    public string? ClientId { get; init; }
    public int? Month { get; init; }
    public int? Year { get; init; }
}