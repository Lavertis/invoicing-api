namespace Invoicing.Domain.Entities;

public class InvoiceItem : BaseEntity
{
    public required string ServiceId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal Value { get; set; }
    public bool IsSuspended { get; set; }
}