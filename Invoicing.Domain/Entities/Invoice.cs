namespace Invoicing.Domain.Entities;

public class Invoice
{
    public Guid Id { get; set; }
    public required string ClientId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public List<InvoiceItem> Items { get; } = [];
}