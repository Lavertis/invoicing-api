using Invoicing.API.Features.Invoices.CreateInvoices;
using Invoicing.API.Features.Invoices.GetInvoices;
using Invoicing.API.Features.Invoices.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Invoicing.API.Controllers;

[ApiController]
[Route("api/invoices")]
public sealed class InvoiceController(IMediator mediator) : BaseController
{
    [HttpPost("generate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateInvoicesCommandResponse>> CreateInvoices(
        [FromBody] CreateInvoicesCommand command
    ) => CreateResponse(await mediator.Send(command));

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<InvoiceResponse>>> GetInvoice([FromQuery] GetInvoicesQuery query)
        => CreateResponse(await mediator.Send(query));
}