using FluentValidation;
using Invoicing.API.Dto.Result;
using Invoicing.Domain.Entities;
using Invoicing.Domain.Enums;
using Invoicing.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicing.API.Features.CalculateInvoices;

public class CalculateInvoicesCommandHandler(InvoicingDbContext context, IValidator<CalculateInvoicesCommand> validator)
    : IRequestHandler<CalculateInvoicesCommand, HttpResult<CalculateInvoicesResponse>>
{
    public async Task<HttpResult<CalculateInvoicesResponse>> Handle(
        CalculateInvoicesCommand request,
        CancellationToken cancellationToken)
    {
        var result = new HttpResult<CalculateInvoicesResponse>();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return result.WithValidationErrors(validationResult.Errors);

        var operations = await context.Operations
            .Where(o => o.Date.Year == request.Year && o.Date.Month == request.Month)
            .OrderBy(o => o.ClientId).ThenBy(o => o.ServiceId).ThenBy(o => o.Date)
            .ToListAsync(cancellationToken);

        var groupedOperations = operations.GroupBy(o => o.ClientId);
        var response = new CalculateInvoicesResponse();

        foreach (var clientGroup in groupedOperations)
        {
            var clientId = clientGroup.Key;
            var existingInvoice = await context.Invoices // TODO: this is not valid
                .Where(i => i.ClientId == clientId)
                .Where(i => i.CreatedAt.Year == request.Year && i.CreatedAt.Month == request.Month)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingInvoice != null)
            {
                response.FailedInvoices.Add(new FailedInvoice
                {
                    ClientId = clientId,
                    Reason = "An invoice already exists for this client in the specified month."
                });
                continue;
            }

            foreach (var serviceGroup in clientGroup.GroupBy(o => o.ServiceId))
            {
                var lastOperation = serviceGroup.LastOrDefault();
                if (lastOperation is not { Type: OperationType.EndService })
                {
                    var failedInvoice = new FailedInvoice
                    {
                        ClientId = clientId,
                        Reason = "The last operation for this service is not an end service."
                    };
                    response.FailedInvoices.Add(failedInvoice);
                    continue;
                }

                var invoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    ClientId = clientId,
                    CreatedAt = DateTime.UtcNow
                };

                InvoiceItem? currentItem = null;

                foreach (var operation in serviceGroup)
                {
                    if (operation.Type is OperationType.StartService or OperationType.ResumeService)
                    {
                        currentItem = new InvoiceItem
                        {
                            ServiceId = operation.ServiceId,
                            StartDate = operation.Date,
                            IsSuspended = false
                        };
                    }
                    else if (operation.Type is OperationType.SuspendService or OperationType.EndService &&
                             currentItem != null)
                    {
                        currentItem.EndDate = operation.Date;
                        currentItem.Value = CalculateValue(currentItem, operation);
                        currentItem.IsSuspended = operation.Type == OperationType.SuspendService;
                        invoice.Items.Add(currentItem);
                        currentItem = null;
                    }
                }

                if (invoice.Items.Count != 0)
                {
                    context.Invoices.Add(invoice);
                    var successfulInvoice = new SuccessfulInvoice { InvoiceId = invoice.Id, ClientId = clientId };
                    response.SuccessfulInvoices.Add(successfulInvoice);
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return result.WithValue(response).WithStatusCode(StatusCodes.Status200OK);
    }

    private decimal CalculateValue(InvoiceItem item, Operation operation)
    {
        var days = (item.EndDate.ToDateTime(TimeOnly.MinValue) - item.StartDate.ToDateTime(TimeOnly.MinValue)).Days;
        return operation.PricePerDay * operation.Quantity * days;
    }
}