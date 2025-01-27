using Carter;
using Invoicing.API.Dto.Common;
using Invoicing.API.Extensions;
using Invoicing.API.Features.Invoices.GenerateInvoices;
using Invoicing.API.Features.Invoices.GetInvoices;
using Invoicing.API.Features.Invoices.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Invoicing.API.Features.Invoices;

public class InvoiceEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var root = app.MapGroup("/api/invoices")
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
    }

    private static async Task<IResult> GenerateInvoices(
        [FromBody] GenerateInvoicesCommand command, [FromServices] IMediator mediator
    ) => (await mediator.Send(command)).CreateResponse();

    private static async Task<IResult> GetInvoices(
        [AsParameters] GetInvoicesQuery query, [FromServices] IMediator mediator
    ) => (await mediator.Send(query)).CreateResponse();
}