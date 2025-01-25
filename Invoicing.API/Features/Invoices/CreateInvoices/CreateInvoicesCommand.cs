using Invoicing.API.Dto.Result;
using MediatR;

namespace Invoicing.API.Features.Invoices.CreateInvoices;

public sealed record CreateInvoicesCommand(int Month, int Year) : IRequest<HttpResult<CreateInvoicesResponse>>;