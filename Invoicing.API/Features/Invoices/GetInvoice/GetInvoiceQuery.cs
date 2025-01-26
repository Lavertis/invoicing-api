using Invoicing.API.Dto.Result;
using Invoicing.API.Features.Invoices.Shared;
using MediatR;

namespace Invoicing.API.Features.Invoices.GetInvoice;

public sealed record GetInvoiceQuery(string ClientId, int Month, int Year) : IRequest<HttpResult<InvoiceResponse>>;