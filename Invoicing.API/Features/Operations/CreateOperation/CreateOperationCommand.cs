using Invoicing.API.Dto.Common;
using Invoicing.API.Dto.Result;
using Invoicing.Domain.Enums;
using MediatR;

namespace Invoicing.API.Features.Operations.CreateOperation;

public sealed record CreateOperationCommand(
    string ServiceId,
    string ClientId,
    int? Quantity,
    decimal? PricePerDay,
    DateOnly Date,
    OperationType Type
) : IRequest<HttpResult<IdResponse<Guid>>>;