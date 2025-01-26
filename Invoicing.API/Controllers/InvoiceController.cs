using Invoicing.API.Dto.Common;
using Invoicing.API.Features.Invoices.GenerateInvoices;
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
    public async Task<ActionResult<GenerateInvoicesCommandResponse>> CreateInvoices(
        [FromBody] GenerateInvoicesCommand command
    ) => CreateResponse(await mediator.Send(command));

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<InvoiceResponse>>> GetInvoice([FromQuery] GetInvoicesQuery query)
        => CreateResponse(await mediator.Send(query));
}