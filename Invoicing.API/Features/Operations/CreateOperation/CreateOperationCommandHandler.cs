using Invoicing.API.Dto.Common;
using Invoicing.API.Dto.Result;
using Invoicing.Domain.Entities;
using Invoicing.Domain.Enums;
using Invoicing.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicing.API.Features.Operations.CreateOperation;

public sealed class CreateOperationCommandHandler(InvoicingDbContext context)
    : IRequestHandler<CreateOperationCommand, HttpResult<IdResponse<Guid>>>
{
    public async Task<HttpResult<IdResponse<Guid>>> Handle(
        CreateOperationCommand request,
        CancellationToken cancellationToken)
    {
        var result = new HttpResult<IdResponse<Guid>>();

        var lastOperation = await FetchLastServiceOperation(request, cancellationToken);
        var isOperationValidResult = IsOperationValid(request, lastOperation);
        if (!isOperationValidResult.Value)
        {
            return result
                .WithStatusCode(StatusCodes.Status400BadRequest)
                .WithError(isOperationValidResult.ErrorMessage);
        }

        var serviceProvision = lastOperation?.ServiceProvision ?? CreateServiceProvision(request);
        var operation = CreateServiceProvisionOperation(request, serviceProvision);
        context.Add(operation);
        await context.SaveChangesAsync(cancellationToken);

        var response = new IdResponse<Guid>(operation.Id);
        return result.WithValue(response).WithStatusCode(StatusCodes.Status201Created);
    }

    private static ServiceProvisionOperation CreateServiceProvisionOperation(
        CreateOperationCommand request,
        ServiceProvision serviceProvision)
    {
        return new ServiceProvisionOperation
        {
            Id = Guid.NewGuid(),
            ServiceProvision = serviceProvision,
            Date = request.Date,
            Type = request.Type
        };
    }

    private async Task<ServiceProvisionOperation?> FetchLastServiceOperation(
        CreateOperationCommand request,
        CancellationToken cancellationToken)
    {
        return await context.ServiceProvisionOperations
            .Include(o => o.ServiceProvision)
            .Where(o =>
                o.ServiceProvision.ClientId == request.ClientId &&
                o.ServiceProvision.ServiceId == request.ServiceId
            )
            .OrderByDescending(o => o.Date)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static ServiceProvision CreateServiceProvision(CreateOperationCommand request)
    {
        return new ServiceProvision
        {
            Id = Guid.NewGuid(),
            ServiceId = request.ServiceId,
            ClientId = request.ClientId,
            Quantity = request.Quantity!.Value,
            PricePerDay = request.PricePerDay!.Value
        };
    }

    private static CommonResult<bool> IsOperationValid(
        CreateOperationCommand request,
        ServiceProvisionOperation? lastOperation)
    {
        if (lastOperation is not null && lastOperation.Date >= request.Date)
        {
            return new CommonResult<bool>()
                .WithValue(false)
                .WithError("The date of the operation must be greater than the date of the last operation.");
        }

        return IsOperationTypeValid(request.Type, lastOperation?.Type);
    }

    private static CommonResult<bool> IsOperationTypeValid(OperationType newOperation, OperationType? lastOperation)
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

    private static CommonResult<bool> ValidateStartService(OperationType? lastOperation, CommonResult<bool> result)
    {
        return lastOperation is null or OperationType.EndService
            ? result.WithValue(true)
            : result
                .WithValue(false)
                .WithError("Cannot start service because the last operation is not an end service.");
    }

    private static CommonResult<bool> ValidateSuspendService(OperationType? lastOperation, CommonResult<bool> result)
    {
        return lastOperation is OperationType.StartService or OperationType.ResumeService
            ? result.WithValue(true)
            : result
                .WithValue(false)
                .WithError("Cannot suspend service because the last operation is not a start or resume service.");
    }

    private static CommonResult<bool> ValidateResumeService(OperationType? lastOperation, CommonResult<bool> result)
    {
        return lastOperation == OperationType.SuspendService
            ? result.WithValue(true)
            : result
                .WithValue(false)
                .WithError("Cannot resume service because the last operation is not a suspend service.");
    }

    private static CommonResult<bool> ValidateEndService(OperationType? lastOperation, CommonResult<bool> result)
    {
        return lastOperation is OperationType.StartService or OperationType.ResumeService
            ? result.WithValue(true)
            : result
                .WithValue(false)
                .WithError("Cannot end service because the last operation is not a start or resume service.");
    }
}