using FluentValidation;
using Invoicing.API.Dto.Result;
using Invoicing.Domain.Entities;
using Invoicing.Domain.Enums;
using Invoicing.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicing.API.Features.Invoices.CreateInvoices;

public class CreateInvoicesCommandHandler(InvoicingDbContext context, IValidator<CreateInvoicesCommand> validator)
    : IRequestHandler<CreateInvoicesCommand, HttpResult<CreateInvoicesResponse>>
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
        foreach (var clientGroup in operations.GroupBy(o => o.ClientId))
        {
            var clientId = clientGroup.Key;
            var isClientAlreadyInvoiced = await IsClientAlreadyInvoicedAsync(
                clientId, request.Year, request.Month, clientGroup, cancellationToken
            );
            if (isClientAlreadyInvoiced) continue;
            ProcessClient(clientGroup);
        }

        await context.SaveChangesAsync(cancellationToken);
        var response = new CreateInvoicesResponse
        {
            SuccessfulInvoices = _successfulInvoices,
            FailedInvoices = _failedInvoices
        };
        return result.WithValue(response).WithStatusCode(StatusCodes.Status200OK);
    }

    private async Task<IEnumerable<Operation>> GetOperations(int year, int month)
    {
        return await context.Operations
            .Where(o => o.Date.Year == year && o.Date.Month == month)
            .OrderBy(o => o.ServiceId).ThenBy(o => o.Date)
            .ToListAsync();
    }

    private async Task<bool> IsClientAlreadyInvoicedAsync(
        string clientId,
        int year,
        int month,
        IGrouping<string, Operation> clientGroup,
        CancellationToken cancellationToken)
    {
        var existingInvoice = await context.Invoices
            .Include(i => i.Items)
            .Where(i => i.ClientId == clientId)
            .Where(i => i.CreatedAt.Year == year && i.CreatedAt.Month == month)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingInvoice == null) return false;

        var areAllOperationsInvoiced = AreAllOperationsInvoiced(clientGroup, existingInvoice);
        if (!areAllOperationsInvoiced)
        {
            LogFailedInvoice(
                clientId,
                "Client has operations that are not invoiced, but an invoice already exists."
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

    private void ProcessClient(IGrouping<string, Operation> clientGroup)
    {
        var clientId = clientGroup.Key;
        var firstOperation = clientGroup.First();
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            CreatedAt = DateTime.UtcNow,
            Year = firstOperation.Date.Year,
            Month = firstOperation.Date.Month
        };
        foreach (var serviceGroup in clientGroup.GroupBy(o => o.ServiceId))
        {
            var processServiceGroupResult = ProcessService(invoice, serviceGroup, clientId);
            if (processServiceGroupResult.IsError) return;
        }

        AddInvoiceIfNotEmpty(invoice, clientId);
    }

    private CommonResult<Unit> ProcessService(
        Invoice invoice,
        IGrouping<string, Operation> serviceGroup, // TODO: probably change to List<Operation> but check what it contains
        string clientId)
    {
        var result = new CommonResult<Unit>();
        var isLastOperationValid = IsLastOperationValid(serviceGroup.LastOrDefault(), clientId);
        if (!isLastOperationValid)
            return result.WithError("The last operation for this service is not an end service.");

        InvoiceItem? currentItem = null;
        // TODO: and then below use Linq instead of foreach
        foreach (var operation in serviceGroup)
        {
            ProcessOperation(operation, invoice, ref currentItem);
        }

        return result;
    }

    private bool IsLastOperationValid(Operation? lastOperation, string clientId)
    {
        if (lastOperation == null || lastOperation.Type == OperationType.EndService)
            return true;

        LogFailedInvoice(
            clientId,
            $"The last operation for {lastOperation.ServiceId} service is not {OperationType.EndService}."
        );
        return false;
    }

    private void ProcessOperation(Operation operation, Invoice invoice, ref InvoiceItem? currentItem)
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
        else if (operation.Type is OperationType.SuspendService or OperationType.EndService && currentItem != null)
        {
            currentItem.EndDate = operation.Date;
            currentItem.Value = CalculateInvoiceItemValue(currentItem, operation);
            currentItem.IsSuspended = operation.Type == OperationType.SuspendService;
            invoice.Items.Add(currentItem);
            currentItem = null;
        }
    }

    private decimal CalculateInvoiceItemValue(InvoiceItem item, Operation operation)
    {
        var startDate = item.StartDate.ToDateTime(TimeOnly.MinValue);
        var endDate = item.EndDate.ToDateTime(TimeOnly.MinValue);
        var days = (endDate - startDate).Days;
        return operation.PricePerDay * operation.Quantity * days;
    }

    private void AddInvoiceIfNotEmpty(Invoice invoice, string clientId)
    {
        if (invoice.Items.Count == 0)
            return;

        context.Invoices.Add(invoice);
        LogSuccessfulInvoice(invoice.Id, clientId);
    }

    private void LogFailedInvoice(string clientId, string reason)
        => _failedInvoices.Add(new FailedInvoice { ClientId = clientId, Reason = reason });

    private void LogSuccessfulInvoice(Guid invoiceId, string clientId)
        => _successfulInvoices.Add(new SuccessfulInvoice { InvoiceId = invoiceId, ClientId = clientId });
}