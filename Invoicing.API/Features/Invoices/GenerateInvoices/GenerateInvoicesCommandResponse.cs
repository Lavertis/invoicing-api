namespace Invoicing.API.Features.Invoices.GenerateInvoices;

public sealed record GenerateInvoicesCommandResponse(
    ICollection<SuccessfulInvoice> SuccessfulInvoices,
    ICollection<FailedInvoice> FailedInvoices
);

public sealed class SuccessfulInvoice
{
    public Guid InvoiceId { get; init; }
    public required string ClientId { get; init; }
}

public sealed class FailedInvoice
{
    public required string ClientId { get; init; }
    public required string Reason { get; init; }
}