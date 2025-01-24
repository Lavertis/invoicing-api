using Invoicing.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Invoicing.Domain.Entities;

[Index(nameof(ClientId), nameof(ServiceId), nameof(Date), IsDescending = [false, false, true])]
public class Operation
{
    public Guid Id { get; set; }
    public required string ServiceId { get; set; }
    public required string ClientId { get; set; }
    public int Quantity { get; set; }
    public decimal? PricePerDay { get; set; }
    public DateOnly Date { get; set; }
    public OperationType Type { get; set; }
}