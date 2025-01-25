using FluentValidation;
using Invoicing.API.Dto.Common;
using Invoicing.API.Dto.Result;
using Invoicing.Domain.Entities;
using Invoicing.Domain.Enums;
using Invoicing.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicing.API.Features.Operations.CreateOperation;

public sealed class CreateOperationCommandHandler(
    InvoicingDbContext context,
    IValidator<CreateOperationCommand> validator)
    : IRequestHandler<CreateOperationCommand, HttpResult<IdResponse<Guid>>>
{
    public async Task<HttpResult<IdResponse<Guid>>> Handle(CreateOperationCommand request,
        CancellationToken cancellationToken)
    {
        var result = new HttpResult<IdResponse<Guid>>();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return result.WithValidationErrors(validationResult.Errors);

        var lastOperation = await context.Operations
            .Where(o => o.ClientId == request.ClientId && o.ServiceId == request.ServiceId)
            .OrderByDescending(o => o.Date)
            .FirstOrDefaultAsync(cancellationToken);

        var isOperationValidResult = IsOperationValid(request, lastOperation);
        if (!isOperationValidResult.Value)
        {
            return result
                .WithStatusCode(StatusCodes.Status400BadRequest)
                .WithError(isOperationValidResult.ErrorMessage);
        }

        var operation = new Operation
        {
            Id = Guid.NewGuid(),
            ServiceId = request.ServiceId,
            ClientId = request.ClientId,
            Quantity = request.Quantity ?? lastOperation!.Quantity,
            PricePerDay = request.PricePerDay ?? lastOperation!.PricePerDay,
            Date = request.Date,
            Type = request.Type
        };
        context.Add(operation);
        await context.SaveChangesAsync(cancellationToken);

        var response = new IdResponse<Guid>(operation.Id);
        return result.WithValue(response).WithStatusCode(StatusCodes.Status201Created);
    }

    private CommonResult<bool> IsOperationValid(CreateOperationCommand request, Operation? lastOperation)
    {
        if (lastOperation is not null && lastOperation.Date >= request.Date)
        {
            return new CommonResult<bool>()
                .WithValue(false)
                .WithError("The date of the operation must be greater than the date of the last operation.");
        }

        return IsOperationTypeValid(request.Type, lastOperation?.Type);
    }

    private CommonResult<bool> IsOperationTypeValid(OperationType newOperation, OperationType? lastOperation)
    {
        var result = new CommonResult<bool>();

        result = newOperation switch
        {
            OperationType.StartService => ValidateStartService(lastOperation, result),
            OperationType.SuspendService => ValidateSuspendService(lastOperation, result),
            OperationType.ResumeService => ValidateResumeService(lastOperation, result),
            OperationType.EndService => ValidateEndService(lastOperation, result),
            _ => result.WithValue(false).WithError("Unexpected operation type.")
        };

        return result;
    }

    private CommonResult<bool> ValidateStartService(OperationType? lastOperation, CommonResult<bool> result)
    {
        return lastOperation is null or OperationType.EndService
            ? result.WithValue(true)
            : result
                .WithValue(false)
                .WithError("Cannot start service because the last operation is not an end service.");
    }

    private CommonResult<bool> ValidateSuspendService(OperationType? lastOperation, CommonResult<bool> result)
    {
        return lastOperation is OperationType.StartService or OperationType.ResumeService
            ? result.WithValue(true)
            : result
                .WithValue(false)
                .WithError("Cannot suspend service because the last operation is not a start or resume service.");
    }

    private CommonResult<bool> ValidateResumeService(OperationType? lastOperation, CommonResult<bool> result)
    {
        return lastOperation == OperationType.SuspendService
            ? result.WithValue(true)
            : result
                .WithValue(false)
                .WithError("Cannot resume service because the last operation is not a suspend service.");
    }

    private CommonResult<bool> ValidateEndService(OperationType? lastOperation, CommonResult<bool> result)
    {
        return lastOperation is OperationType.StartService or OperationType.ResumeService
            ? result.WithValue(true)
            : result
                .WithValue(false)
                .WithError("Cannot end service because the last operation is not a start or resume service.");
    }
}