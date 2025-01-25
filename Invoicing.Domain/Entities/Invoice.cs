namespace Invoicing.Domain.Entities;

public class Invoice
{
    public Guid Id { get; set; }
    public required string ClientId { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<InvoiceItem> Items { get; set; } = [];
}