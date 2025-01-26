using FluentValidation;
using Invoicing.API.Dto.Result;
using Invoicing.Domain.Entities;
using Invoicing.Domain.Enums;
using Invoicing.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicing.API.Features.Invoices.CreateInvoices;

public sealed class CreateInvoicesCommandHandler(
    InvoicingDbContext context,
    IValidator<CreateInvoicesCommand> validator
) : IRequestHandler<CreateInvoicesCommand, HttpResult<CreateInvoicesResponse>>
{
    private readonly List<FailedInvoice> _failedInvoices = [];
    private readonly List<SuccessfulInvoice> _successfulInvoices = [];

    public async Task<HttpResult<CreateInvoicesResponse>> Handle(
        CreateInvoicesCommand request,
        CancellationToken cancellationToken)
    {
        var result = new HttpResult<CreateInvoicesResponse>();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return result.WithValidationErrors(validationResult.Errors);

        var clientIds = await GetClientIdsWithOperations(request.Year, request.Month);
        foreach (var clientId in clientIds)
        {
            var clientOperations = await GetOperationsForClient(clientId, request.Year, request.Month);
            var existingInvoice = await GetExistingInvoiceAsync(clientId, request.Year, request.Month);
            if (existingInvoice != null)
            {
                ValidateAllClientOperationsAreInvoiced(clientOperations, existingInvoice);
                continue;
            }

            var createInvoiceResult = CreateInvoiceForClient(clientOperations);
            if (createInvoiceResult.IsError)
            {
                LogFailedInvoice(clientId, createInvoiceResult.ErrorMessage);
                continue;
            }

            var invoice = createInvoiceResult.Value!;
            context.Invoices.Add(invoice);
            LogSuccessfulInvoice(invoice.Id, invoice.ClientId);
        }

        await context.SaveChangesAsync(cancellationToken);
        var response = new CreateInvoicesResponse(_successfulInvoices, _failedInvoices);
        return result.WithValue(response).WithStatusCode(StatusCodes.Status200OK);
    }

    private async Task<List<string>> GetClientIdsWithOperations(int year, int month)
    {
        return await context.ServiceProvisionOperations
            .Where(o => o.Date.Year == year && o.Date.Month == month)
            .Select(o => o.ServiceProvision.ClientId)
            .Distinct()
            .ToListAsync();
    }

    private async Task<Invoice?> GetExistingInvoiceAsync(string clientId, int year, int month)
    {
        return await context.Invoices
            .Include(i => i.Items)
            .Where(i => i.ClientId == clientId)
            .Where(i => i.Year == year && i.Month == month)
            .FirstOrDefaultAsync();
    }

    private async Task<IList<ServiceProvisionOperation>> GetOperationsForClient(string clientId, int year, int month)
    {
        return await context.ServiceProvisionOperations
            .Include(o => o.ServiceProvision)
            .Where(o => o.ServiceProvision.ClientId == clientId && o.Date.Year == year && o.Date.Month == month)
            .OrderBy(o => o.Date)
            .ToListAsync();
    }

    private void ValidateAllClientOperationsAreInvoiced(IList<ServiceProvisionOperation> operations, Invoice invoice)
    {
        var areAllOperationsInvoiced = AreAllOperationsInvoiced(operations, invoice);
        if (!areAllOperationsInvoiced)
        {
            LogFailedInvoice(
                invoice.ClientId, "Client has operations that are not invoiced, but an invoice already exists."
            );
        }
    }

    private bool AreAllOperationsInvoiced(IEnumerable<ServiceProvisionOperation> operations, Invoice invoice)
    {
        return operations
            .All(operation => invoice.Items
                .Any(item =>
                    item.ServiceId == operation.ServiceProvision.ServiceId &&
                    item.StartDate <= operation.Date &&
                    item.EndDate >= operation.Date
                )
            );
    }

    private CommonResult<Invoice?> CreateInvoiceForClient(IList<ServiceProvisionOperation> operations)
    {
        var result = new CommonResult<Invoice?>();
        var firstOperation = operations.First();
        var clientId = firstOperation.ServiceProvision.ClientId;
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            CreatedAt = DateTime.UtcNow,
            Year = firstOperation.Date.Year,
            Month = firstOperation.Date.Month
        };

        foreach (var serviceOperationsGroup in operations.GroupBy(o => o.ServiceProvision.ServiceId))
        {
            if (operations.Last().Type != OperationType.EndService)
            {
                var errorMsg = $"The last operation for client {clientId} for service " +
                               $"{serviceOperationsGroup.Key} is not {OperationType.EndService}.";
                LogFailedInvoice(clientId, errorMsg);
                return result.WithError(errorMsg);
            }

            var invoiceItems = CreateInvoiceItemsForService(serviceOperationsGroup);
            invoice.Items.AddRange(invoiceItems);
        }

        return invoice.Items.Count == 0
            ? result.WithError($"Invoice items list is empty for client {clientId}.")
            : result.WithValue(invoice);
    }

    private IEnumerable<InvoiceItem> CreateInvoiceItemsForService(IEnumerable<ServiceProvisionOperation> operations)
    {
        return operations
            .Chunk(2)
            .Select(operationsChunk => CreateInvoiceItem(operationsChunk[0], operationsChunk[1]));
    }

    private InvoiceItem CreateInvoiceItem(
        ServiceProvisionOperation beginOperation,
        ServiceProvisionOperation endOperation)
    {
        var invoiceItem = new InvoiceItem
        {
            ServiceId = beginOperation.ServiceProvision.ServiceId,
            StartDate = beginOperation.Date,
            EndDate = endOperation.Date,
            IsSuspended = endOperation.Type == OperationType.SuspendService,
            Value = CalculateInvoiceItemValue(beginOperation, endOperation)
        };
        return invoiceItem;
    }

    private decimal CalculateInvoiceItemValue(
        ServiceProvisionOperation beginOperation,
        ServiceProvisionOperation endOperation)
    {
        var startDate = beginOperation.Date.ToDateTime(TimeOnly.MinValue);
        var endDate = endOperation.Date.ToDateTime(TimeOnly.MinValue);
        var days = (endDate - startDate).Days;
        return beginOperation.ServiceProvision.PricePerDay * beginOperation.ServiceProvision.Quantity * days;
    }

    private void LogFailedInvoice(string clientId, string? reason)
        => _failedInvoices.Add(new FailedInvoice(clientId, reason ?? "Unknown reason"));

    private void LogSuccessfulInvoice(Guid invoiceId, string clientId)
        => _successfulInvoices.Add(new SuccessfulInvoice(invoiceId, clientId));
}