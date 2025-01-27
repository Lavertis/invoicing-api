using Invoicing.API.Dto.Common;
using Invoicing.API.Dto.Result;
using Invoicing.Domain.Entities;
using Invoicing.Domain.Enums;
using Invoicing.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicing.API.Features.ServiceOperations.CreateServiceOperation;

public sealed class CreateServiceOperationCommandHandler(InvoicingDbContext context)
    : IRequestHandler<CreateServiceOperationCommand, HttpResult<IdResponse<Guid>>>
{
    public async Task<HttpResult<IdResponse<Guid>>> Handle(
        CreateServiceOperationCommand request,
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

    private static ServiceOperation CreateServiceProvisionOperation(
        CreateServiceOperationCommand request,
        ServiceProvision serviceProvision)
    {
        return new ServiceOperation
        {
            Id = Guid.NewGuid(),
            ServiceProvision = serviceProvision,
            Date = request.Date,
            Type = request.Type
        };
    }

    private async Task<ServiceOperation?> FetchLastServiceOperation(
        CreateServiceOperationCommand request,
        CancellationToken cancellationToken)
    {
        return await context.ServiceOperations
            .Include(o => o.ServiceProvision)
            .Where(o =>
                o.ServiceProvision.ClientId == request.ClientId &&
                o.ServiceProvision.ServiceId == request.ServiceId
            )
            .OrderByDescending(o => o.Date)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static ServiceProvision CreateServiceProvision(CreateServiceOperationCommand request)
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
        CreateServiceOperationCommand request,
        ServiceOperation? lastOperation)
    {
        if (lastOperation is not null && lastOperation.Date >= request.Date)
        {
            return new CommonResult<bool>()
                .WithValue(false)
                .WithError("The date of the operation must be greater than the date of the last operation.");
        }

        return IsOperationTypeValid(request.Type, lastOperation?.Type);
    }

    private static CommonResult<bool> IsOperationTypeValid(ServiceOperationType newOperation,
        ServiceOperationType? lastOperation)
    {
        var result = new CommonResult<bool>();

        result = newOperation switch
        {
            ServiceOperationType.Start => ValidateStartService(lastOperation, result),
            ServiceOperationType.Suspend => ValidateSuspendService(lastOperation, result),
            ServiceOperationType.Resume => ValidateResumeService(lastOperation, result),
            ServiceOperationType.End => ValidateEndService(lastOperation, result),
            _ => result.WithValue(false).WithError("Unexpected operation type.")
        };

        return result;
    }

    private static CommonResult<bool> ValidateStartService(ServiceOperationType? lastOperation,
        CommonResult<bool> result)
    {
        return lastOperation is null or ServiceOperationType.End
            ? result.WithValue(true)
            : result
                .WithValue(false)
                .WithError("Cannot start service because the last operation is not an end service.");
    }

    private static CommonResult<bool> ValidateSuspendService(ServiceOperationType? lastOperation,
        CommonResult<bool> result)
    {
        return lastOperation is ServiceOperationType.Start or ServiceOperationType.Resume
            ? result.WithValue(true)
            : result
                .WithValue(false)
                .WithError("Cannot suspend service because the last operation is not a start or resume service.");
    }

    private static CommonResult<bool> ValidateResumeService(ServiceOperationType? lastOperation,
        CommonResult<bool> result)
    {
        return lastOperation == ServiceOperationType.Suspend
            ? result.WithValue(true)
            : result
                .WithValue(false)
                .WithError("Cannot resume service because the last operation is not a suspend service.");
    }

    private static CommonResult<bool> ValidateEndService(ServiceOperationType? lastOperation, CommonResult<bool> result)
    {
        return lastOperation is ServiceOperationType.Start or ServiceOperationType.Resume
            ? result.WithValue(true)
            : result
                .WithValue(false)
                .WithError("Cannot end service because the last operation is not a start or resume service.");
    }
}