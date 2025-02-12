using Microsoft.EntityFrameworkCore;

namespace Invoicing.Domain.Entities;

[Index(nameof(ServiceId), nameof(ClientId))]
public sealed class ServiceProvision : BaseEntity
{
    public required string ServiceId { get; set; }
    public required string ClientId { get; set; }
    public required int Quantity { get; set; }
    public required decimal PricePerDay { get; set; }
    public List<ServiceOperation> Operations { get; set; } = [];
}