using Invoicing.API.Dto.Result;
using MediatR;

namespace Invoicing.API.Features.Invoices.CreateInvoices;

public sealed class CreateInvoicesCommand : IRequest<HttpResult<CreateInvoicesCommandResponse>>
{
    public int Month { get; init; }
    public int Year { get; init; }
}