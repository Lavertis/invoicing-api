using Invoicing.API.Dto.Result;
using MediatR;

namespace Invoicing.API.Features.CalculateInvoices;

public class CalculateInvoicesCommand : IRequest<HttpResult<CalculateInvoicesResponse>>
{
    public int Month { get; set; }
    public int Year { get; set; }
}