using Invoicing.API.Dto.Common;
using Invoicing.API.Extensions;
using Invoicing.API.Features.Invoices.GenerateInvoices;
using Invoicing.API.Features.Invoices.GetInvoices;
using Invoicing.API.Features.Invoices.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Invoicing.API.Endpoints.Routes;

public static class InvoiceEndpoints
{
    public static WebApplication MapInvoiceEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/invoices")
            // .AddEndpointFilterFactory(ValidationFilter.ValidationFilterFactory)
            .WithTags("Invoice")
            .WithOpenApi();

        _ = root.MapPost("/generate", GenerateInvoices)
            .Produces<GenerateInvoicesCommandResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithSummary("Generates invoices for each client in a given month.")
            .WithDescription("Returns lists of successfully and unsuccessfully generated invoices.");

        _ = root.MapGet("/", GetInvoices)
            .Produces<PaginatedResponse<InvoiceResponse>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithSummary("Retrieves a list of invoices.")
            .WithDescription("Returns a paginated list of invoices.");

        return app;
    }

    private static async Task<IResult> GenerateInvoices(
        [FromBody] GenerateInvoicesCommand command, [FromServices] IMediator mediator
    ) => (await mediator.Send(command)).CreateResponse();

    private static async Task<IResult> GetInvoices(
        [AsParameters] GetInvoicesQuery query, [FromServices] IMediator mediator
    ) => (await mediator.Send(query)).CreateResponse();
}