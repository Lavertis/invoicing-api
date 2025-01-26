using System.ComponentModel.DataAnnotations;

namespace Invoicing.Domain.Entities;

public class InvoiceItem : BaseEntity
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal Value { get; set; }
    public bool IsSuspended { get; set; }

    [Required] public required string ServiceId { get; set; }
    [Required] public Guid InvoiceId { get; set; }
}