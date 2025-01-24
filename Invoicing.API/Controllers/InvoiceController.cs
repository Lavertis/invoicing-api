using Invoicing.API.CQRS.Commands.AddOperation;
using Invoicing.API.Dto.Common;
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
}