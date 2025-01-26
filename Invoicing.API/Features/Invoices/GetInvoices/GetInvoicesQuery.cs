using Invoicing.API.Dto.Common;
using Invoicing.API.Dto.Result;
using Invoicing.API.Features.Invoices.Shared;
using MediatR;

namespace Invoicing.API.Features.Invoices.GetInvoices;

public sealed record GetInvoicesQuery(string? ClientId, int? Month, int? Year)
    : PaginationQuery, IRequest<HttpResult<PaginatedResponse<InvoiceResponse>>>;