using Invoicing.API.Dto.Common;
using Invoicing.API.Extensions;
using Invoicing.API.Features.Operations.CreateOperation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Invoicing.API.Endpoints.Routes;

public static class OperationEndpoints
{
    public static WebApplication MapOperationEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/operations")
            // .AddEndpointFilterFactory(ValidationFilter.ValidationFilterFactory)
            .WithTags("Operation")
            .WithOpenApi();

        _ = root.MapPost("/", CreateOperation)
            .Produces<IdResponse<Guid>>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithSummary("Adds a new service provision operation for a given Client")
            .WithDescription("Quantity and pricePerDay fields are provided only for the 'Start' operation type.");

        return app;
    }

    private static async Task<IResult> CreateOperation(
        [FromBody] CreateOperationCommand command, [FromServices] IMediator mediator
    ) => (await mediator.Send(command)).CreateResponse();
}