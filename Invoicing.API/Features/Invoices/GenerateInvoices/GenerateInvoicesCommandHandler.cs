using Invoicing.API.Dto.Result;
using Invoicing.Domain.Entities;
using Invoicing.Domain.Enums;
using Invoicing.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicing.API.Features.Invoices.GenerateInvoices;

public sealed class GenerateInvoicesCommandHandler(ApplicationDbContext context)
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
            var createInvoiceResult = await CreateInvoice(request, clientId);
            if (createInvoiceResult.Data != null)
                LogSuccessfulInvoice(createInvoiceResult.Data.Value, clientId);
            else if (createInvoiceResult.IsError)
                LogFailedInvoice(clientId, createInvoiceResult.ErrorMessage);
        }

        await context.SaveChangesAsync(cancellationToken);
        var response = new GenerateInvoicesCommandResponse(_successfulInvoices, _failedInvoices);
        return result.WithData(response).WithStatusCode(StatusCodes.Status200OK);
    }

    private async Task<CommonResult<Guid?>> CreateInvoice(GenerateInvoicesCommand request, string clientId)
    {
        var result = new CommonResult<Guid?>();
        var clientOperations = await GetOperationsForClient(clientId, request.Year, request.Month);
        var existingInvoice = await GetExistingInvoiceAsync(clientId, request.Year, request.Month);
        if (existingInvoice != null)
        {
            var areAllOperationsInvoicedResult = VerifyAllOperationsAreInvoiced(existingInvoice, clientOperations);
            if (areAllOperationsInvoicedResult is { Data: false, IsError: true })
                return result.WithError(areAllOperationsInvoicedResult.ErrorMessage);
            return result;
        }

        var createInvoiceResult = CreateInvoiceForClient(clientOperations);
        if (createInvoiceResult.IsError)
            return result.WithError(createInvoiceResult.ErrorMessage);

        var invoice = createInvoiceResult.Data!;
        context.Invoices.Add(invoice);
        return result.WithData(invoice.Id);
    }

    private async Task<List<string>> GetClientIdsWithOperations(int year, int month)
    {
        return await context.ServiceOperations
            .Where(o => o.Date.Year == year && o.Date.Month == month)
            .Select(o => o.ServiceProvision.ClientId)
            .Distinct()
            .ToListAsync();
    }

    private static CommonResult<bool> VerifyAllOperationsAreInvoiced(
        Invoice invoice,
        IList<ServiceOperation> clientOperations)
    {
        var result = new CommonResult<bool>();
        var areAllOperationsInvoiced = AreAllOperationsInvoiced(clientOperations, invoice);
        return areAllOperationsInvoiced
            ? result.WithData(true)
            : result
                .WithData(false)
                .WithError("Client has operations that are not invoiced, but an invoice already exists.");
    }

    private static bool AreAllOperationsInvoiced(IEnumerable<ServiceOperation> operations, Invoice invoice)
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

    private async Task<IList<ServiceOperation>> GetOperationsForClient(string clientId, int year, int month)
    {
        return await context.ServiceOperations
            .Include(o => o.ServiceProvision)
            .Where(o => o.ServiceProvision.ClientId == clientId && o.Date.Year == year && o.Date.Month == month)
            .OrderBy(o => o.Date)
            .ToListAsync();
    }

    private static CommonResult<Invoice?> CreateInvoiceForClient(IList<ServiceOperation> operations)
    {
        var result = new CommonResult<Invoice?>();
        var firstOperation = operations[0];
        var clientId = firstOperation.ServiceProvision.ClientId;
        var invoice = CreateInvoice(clientId, firstOperation.Date);

        foreach (var serviceOperationsGroup in operations.GroupBy(o => o.ServiceProvision.ServiceId))
        {
            if (serviceOperationsGroup.Last().Type != ServiceOperationType.End)
            {
                return result.WithError(
                    $"The last operation for client {clientId} for service " +
                    $"{serviceOperationsGroup.Key} is not {ServiceOperationType.End}."
                );
            }

            var invoiceItems = CreateInvoiceItemsForService(serviceOperationsGroup);
            invoice.Items.AddRange(invoiceItems);
        }

        return invoice.Items.Count == 0
            ? result.WithError($"Invoice items list is empty for client {clientId}.")
            : result.WithData(invoice);
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
        IEnumerable<ServiceOperation> operations)
    {
        return operations
            .Chunk(2)
            .Select(operationsChunk => CreateInvoiceItem(operationsChunk[0], operationsChunk[1]));
    }

    private static InvoiceItem CreateInvoiceItem(
        ServiceOperation beginOperation,
        ServiceOperation endOperation)
    {
        var invoiceItem = new InvoiceItem
        {
            ServiceId = beginOperation.ServiceProvision.ServiceId,
            StartDate = beginOperation.Date,
            EndDate = endOperation.Date,
            IsSuspended = endOperation.Type == ServiceOperationType.Suspend,
            Value = CalculateInvoiceItemValue(beginOperation, endOperation)
        };
        return invoiceItem;
    }

    private static decimal CalculateInvoiceItemValue(
        ServiceOperation beginOperation,
        ServiceOperation endOperation)
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