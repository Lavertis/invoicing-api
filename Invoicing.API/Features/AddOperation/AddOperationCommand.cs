using Invoicing.API.Dto.Common;
using Invoicing.API.Dto.Result;
using Invoicing.Domain.Enums;
using MediatR;

namespace Invoicing.API.Features.AddOperation;

public class AddOperationCommand : IRequest<HttpResult<IdResponse<Guid>>>
{
    public required string ServiceId { get; init; }
    public required string ClientId { get; init; }
    public int? Quantity { get; init; }
    public decimal? PricePerDay { get; init; }
    public required DateOnly Date { get; init; }
    public required OperationType Type { get; init; }
}