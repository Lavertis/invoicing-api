using Invoicing.API.Features.Invoices.CreateInvoices;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Invoicing.API.Controllers;

[ApiController]
[Route("api/invoices")]
public sealed class InvoiceController(IMediator mediator) : BaseController
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<CreateInvoicesResponse>> CreateInvoices(
        [FromBody] CreateInvoicesCommand command
    ) => CreateResponse(await mediator.Send(command));
}