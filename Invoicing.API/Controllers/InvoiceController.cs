using Invoicing.API.Features.Invoices.CreateInvoices;
using Invoicing.API.Features.Invoices.GetInvoice;
using Invoicing.API.Features.Invoices.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Invoicing.API.Controllers;

[ApiController]
[Route("api/invoices")]
public sealed class InvoiceController(IMediator mediator) : BaseController
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateInvoicesCommandResponse>> CreateInvoices(
        [FromBody] CreateInvoicesCommand command
    ) => CreateResponse(await mediator.Send(command));

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceResponse>> GetInvoice([FromQuery] GetInvoiceQuery query)
        => CreateResponse(await mediator.Send(query));
}