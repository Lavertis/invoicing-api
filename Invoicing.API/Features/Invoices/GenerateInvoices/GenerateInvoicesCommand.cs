using Invoicing.API.Dto.Result;
using MediatR;

namespace Invoicing.API.Features.Invoices.GenerateInvoices;

public sealed class GenerateInvoicesCommand : IRequest<HttpResult<GenerateInvoicesCommandResponse>>
{
    public int Month { get; init; }
    public int Year { get; init; }
}