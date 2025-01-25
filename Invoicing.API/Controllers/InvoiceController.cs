using Invoicing.API.Dto.Common;
using Invoicing.API.Features.AddOperation;
using Invoicing.API.Features.CalculateInvoices;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Invoicing.API.Controllers;

[ApiController]
[Route("invoices")]
public class InvoiceController(IMediator mediator) : BaseController
{
    [HttpPost("add-operation")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<IdResponse<Guid>>> AddOperation([FromBody] AddOperationCommand command)
        => CreateResponse(await mediator.Send(command));

    [HttpPost("calculate-invoices")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<CalculateInvoicesResponse>> CalculateInvoices(
        [FromBody] CalculateInvoicesCommand command
    ) => CreateResponse(await mediator.Send(command));
}