using Invoicing.API.Dto.Common;
using Invoicing.API.Features.Operations.CreateOperation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Invoicing.API.Controllers;

[ApiController]
[Route("api/operations")]
public sealed class OperationController(IMediator mediator) : BaseController
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<IdResponse<Guid>>> CreateOperation([FromBody] CreateOperationCommand command)
        => CreateResponse(await mediator.Send(command));
}