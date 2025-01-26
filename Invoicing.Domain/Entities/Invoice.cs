using Microsoft.EntityFrameworkCore;

namespace Invoicing.Domain.Entities;

[Index(nameof(ClientId), nameof(Year), nameof(Month), IsUnique = true)]
public sealed class Invoice : BaseEntity
{
    public required string ClientId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public List<InvoiceItem> Items { get; } = [];
}