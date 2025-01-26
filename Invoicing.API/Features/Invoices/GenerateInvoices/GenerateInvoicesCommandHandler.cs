using Invoicing.API.Dto.Result;
using Invoicing.Domain.Entities;
using Invoicing.Domain.Enums;
using Invoicing.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicing.API.Features.Invoices.GenerateInvoices;

public sealed class GenerateInvoicesCommandHandler(InvoicingDbContext context)
    : IRequestHandler<GenerateInvoicesCommand, HttpResult<GenerateInvoicesCommandResponse>>
{
    private readonly List<FailedInvoice> _failedInvoices = [];
    private readonly List<SuccessfulInvoice> _successfulInvoices = [];

    public async Task<HttpResult<GenerateInvoicesCommandResponse>> Handle(
        GenerateInvoicesCommand request,
        CancellationToken cancellationToken)
    {
        var result = new HttpResult<GenerateInvoicesCommandResponse>();

        var clientIds = await GetClientIdsWithOperations(request.Year, request.Month);
        foreach (var clientId in clientIds)
        {
            var clientOperations = await GetOperationsForClient(clientId, request.Year, request.Month);
            var existingInvoice = await GetExistingInvoiceAsync(clientId, request.Year, request.Month);
            if (existingInvoice != null)
            {
                var areAllOperationsInvoicedResult = VerifyAllOperationsAreInvoiced(existingInvoice, clientOperations);
                if (areAllOperationsInvoicedResult is { Value: false, IsError: true })
                {
                    LogFailedInvoice(clientId, areAllOperationsInvoicedResult.ErrorMessage);
                    continue;
                }
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
        var response = new GenerateInvoicesCommandResponse(_successfulInvoices, _failedInvoices);
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

    private static CommonResult<bool> VerifyAllOperationsAreInvoiced(
        Invoice invoice,
        IList<ServiceProvisionOperation> clientOperations)
    {
        var result = new CommonResult<bool>();
        var areAllOperationsInvoiced = AreAllOperationsInvoiced(clientOperations, invoice);
        return areAllOperationsInvoiced
            ? result.WithValue(true)
            : result
                .WithValue(false)
                .WithError("Client has operations that are not invoiced, but an invoice already exists.");
    }

    private static bool AreAllOperationsInvoiced(IEnumerable<ServiceProvisionOperation> operations, Invoice invoice)
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

    private static CommonResult<Invoice?> CreateInvoiceForClient(IList<ServiceProvisionOperation> operations)
    {
        var result = new CommonResult<Invoice?>();
        var firstOperation = operations[0];
        var clientId = firstOperation.ServiceProvision.ClientId;
        var invoice = CreateInvoice(clientId, firstOperation.Date);

        foreach (var serviceOperationsGroup in operations.GroupBy(o => o.ServiceProvision.ServiceId))
        {
            if (serviceOperationsGroup.Last().Type != OperationType.EndService)
            {
                return result.WithError(
                    $"The last operation for client {clientId} for service " +
                    $"{serviceOperationsGroup.Key} is not {OperationType.EndService}."
                );
            }

            var invoiceItems = CreateInvoiceItemsForService(serviceOperationsGroup);
            invoice.Items.AddRange(invoiceItems);
        }

        return invoice.Items.Count == 0
            ? result.WithError($"Invoice items list is empty for client {clientId}.")
            : result.WithValue(invoice);
    }

    private static Invoice CreateInvoice(string clientId, DateOnly date)
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            CreatedAt = DateTime.UtcNow,
            Year = date.Year,
            Month = date.Month
        };
        return invoice;
    }

    private static IEnumerable<InvoiceItem> CreateInvoiceItemsForService(
        IEnumerable<ServiceProvisionOperation> operations)
    {
        return operations
            .Chunk(2)
            .Select(operationsChunk => CreateInvoiceItem(operationsChunk[0], operationsChunk[1]));
    }

    private static InvoiceItem CreateInvoiceItem(
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

    private static decimal CalculateInvoiceItemValue(
        ServiceProvisionOperation beginOperation,
        ServiceProvisionOperation endOperation)
    {
        var startDate = beginOperation.Date.ToDateTime(TimeOnly.MinValue);
        var endDate = endOperation.Date.ToDateTime(TimeOnly.MinValue);
        var days = (endDate - startDate).Days;
        return beginOperation.ServiceProvision.PricePerDay * beginOperation.ServiceProvision.Quantity * days;
    }

    private void LogFailedInvoice(string clientId, string? reason)
        => _failedInvoices.Add(new FailedInvoice { ClientId = clientId, Reason = reason ?? "Unknown reason" });

    private void LogSuccessfulInvoice(Guid invoiceId, string clientId)
        => _successfulInvoices.Add(new SuccessfulInvoice { InvoiceId = invoiceId, ClientId = clientId });
}