namespace Invoicing.API.Features.CalculateInvoices;

public class CalculateInvoicesResponse
{
    public ICollection<SuccessfulInvoice> SuccessfulInvoices { get; set; } = [];
    public ICollection<FailedInvoice> FailedInvoices { get; set; } = [];
}

public class SuccessfulInvoice
{
    public required Guid InvoiceId { get; set; }
    public required string ClientId { get; set; }
}

public class FailedInvoice
{
    public required string ClientId { get; set; }
    public required string Reason { get; set; }
}