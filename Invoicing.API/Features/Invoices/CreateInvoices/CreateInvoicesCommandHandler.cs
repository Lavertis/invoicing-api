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

        var operations = await GetOperations(request.Year, request.Month);
        foreach (var clientOperationsGroup in operations.GroupBy(o => o.ClientId))
        {
            var isClientAlreadyInvoiced = await IsClientAlreadyInvoicedAsync(
                request.Year, request.Month, clientOperationsGroup, cancellationToken
            );
            if (isClientAlreadyInvoiced) continue;
            ProcessClient(clientOperationsGroup);
        }

        await context.SaveChangesAsync(cancellationToken);
        var response = new CreateInvoicesResponse(_successfulInvoices, _failedInvoices);
        return result.WithValue(response).WithStatusCode(StatusCodes.Status200OK);
    }

    private async Task<IEnumerable<Operation>> GetOperations(int year, int month)
    {
        return await context.Operations // TODO: process one client at a time
            .Where(o => o.Date.Year == year && o.Date.Month == month)
            .OrderBy(o => o.Date)
            .ToListAsync();
    }

    private async Task<bool> IsClientAlreadyInvoicedAsync(
        int year,
        int month, // TODO refactor
        IGrouping<string, Operation> clientOperationsGroup,
        CancellationToken cancellationToken)
    {
        var clientId = clientOperationsGroup.Key;
        var existingInvoice = await context.Invoices
            .Include(i => i.Items)
            .Where(i => i.ClientId == clientId)
            .Where(i => i.Year == year && i.Month == month)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingInvoice == null) return false;

        var areAllOperationsInvoiced = AreAllOperationsInvoiced(clientOperationsGroup, existingInvoice);
        if (!areAllOperationsInvoiced)
        {
            LogFailedInvoice(
                clientId, "Client has operations that are not invoiced, but an invoice already exists."
            );
        }

        return true;
    }

    private bool AreAllOperationsInvoiced(IEnumerable<Operation> operations, Invoice invoice)
    {
        return operations
            .All(operation => invoice.Items
                .Any(item =>
                    item.ServiceId == operation.ServiceId &&
                    item.StartDate <= operation.Date &&
                    item.EndDate >= operation.Date
                )
            );
    }

    private void ProcessClient(IGrouping<string, Operation> clientOperationsGroup)
    {
        var clientId = clientOperationsGroup.Key;
        var firstOperation = clientOperationsGroup.First();
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            CreatedAt = DateTime.UtcNow,
            Year = firstOperation.Date.Year,
            Month = firstOperation.Date.Month
        };
        foreach (var serviceOperationsGroup in clientOperationsGroup.GroupBy(o => o.ServiceId))
        {
            var processServiceGroupResult = ProcessService(serviceOperationsGroup.ToList(), invoice, clientId);
            if (processServiceGroupResult.IsError) return;
        }

        AddInvoiceIfNotEmpty(invoice, clientId);
    }

    private CommonResult<Unit> ProcessService(IList<Operation> serviceOperations, Invoice invoice, string clientId)
    {
        var result = new CommonResult<Unit>();
        var isLastOperationValid = IsLastOperationValid(serviceOperations.LastOrDefault(), clientId);
        if (!isLastOperationValid)
            return result.WithError("The last operation for this service is not an end service.");

        foreach (var operations in serviceOperations.Chunk(2))
        {
            var invoiceItem = CreateInvoiceItem(operations[0], operations[1]);
            invoice.Items.Add(invoiceItem);
        }

        return result;
    }

    private bool IsLastOperationValid(Operation? lastOperation, string clientId)
    {
        if (lastOperation == null || lastOperation.Type == OperationType.EndService)
            return true;

        LogFailedInvoice(
            clientId, $"The last operation for {lastOperation.ServiceId} service is not {OperationType.EndService}."
        );
        return false;
    }

    private InvoiceItem CreateInvoiceItem(Operation beginOperation, Operation endOperation)
    {
        var invoiceItem = new InvoiceItem
        {
            ServiceId = beginOperation.ServiceId,
            StartDate = beginOperation.Date,
            EndDate = endOperation.Date,
            IsSuspended = endOperation.Type == OperationType.SuspendService,
            Value = CalculateInvoiceItemValue(beginOperation, endOperation)
        };
        return invoiceItem;
    }

    private decimal CalculateInvoiceItemValue(Operation beginOperation, Operation endOperation)
    {
        var startDate = beginOperation.Date.ToDateTime(TimeOnly.MinValue);
        var endDate = endOperation.Date.ToDateTime(TimeOnly.MinValue);
        var days = (endDate - startDate).Days;
        return beginOperation.PricePerDay * beginOperation.Quantity * days;
    }

    private void AddInvoiceIfNotEmpty(Invoice invoice, string clientId)
    {
        if (invoice.Items.Count == 0)
            return;

        context.Invoices.Add(invoice);
        LogSuccessfulInvoice(invoice.Id, clientId);
    }

    private void LogFailedInvoice(string clientId, string reason)
        => _failedInvoices.Add(new FailedInvoice(clientId, reason));

    private void LogSuccessfulInvoice(Guid invoiceId, string clientId)
        => _successfulInvoices.Add(new SuccessfulInvoice(invoiceId, clientId));
}