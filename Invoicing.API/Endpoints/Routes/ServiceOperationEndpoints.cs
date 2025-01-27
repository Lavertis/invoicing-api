using Invoicing.API.Dto.Common;
using Invoicing.API.Extensions;
using Invoicing.API.Features.ServiceOperations.CreateServiceOperation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Invoicing.API.Endpoints.Routes;

public static class ServiceOperationEndpoints
{
    public static WebApplication MapServiceOperationEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/service-operations")
            .WithTags("Service Operation")
            .WithOpenApi();

        _ = root.MapPost("/", CreateServiceOperation)
            .Produces<IdResponse<Guid>>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithSummary("Adds a new service operation for a given Client")
            .WithDescription("Quantity and pricePerDay fields are provided only for the 'Start' operation type.");

        return app;
    }

    private static async Task<IResult> CreateServiceOperation(
        [FromBody] CreateServiceOperationCommand command, [FromServices] IMediator mediator
    ) => (await mediator.Send(command)).CreateResponse();
}