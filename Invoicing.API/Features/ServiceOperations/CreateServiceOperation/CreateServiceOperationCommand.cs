using Invoicing.API.Dto.Common;
using Invoicing.API.Dto.Result;
using Invoicing.Domain.Enums;
using MediatR;

namespace Invoicing.API.Features.ServiceOperations.CreateServiceOperation;

public sealed class CreateServiceOperationCommand : IRequest<HttpResult<IdResponse<Guid>>>
{
    public required string ServiceId { get; init; }
    public required string ClientId { get; init; }
    public int? Quantity { get; init; }
    public decimal? PricePerDay { get; init; }
    public DateOnly Date { get; init; }
    public ServiceOperationType Type { get; init; }
}