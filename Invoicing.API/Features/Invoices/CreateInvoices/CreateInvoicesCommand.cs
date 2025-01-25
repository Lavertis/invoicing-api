using Invoicing.API.Dto.Result;
using MediatR;

namespace Invoicing.API.Features.Invoices.CreateInvoices;

public class CreateInvoicesCommand : IRequest<HttpResult<CreateInvoicesResponse>>
{
    public int Month { get; set; }
    public int Year { get; set; }
}