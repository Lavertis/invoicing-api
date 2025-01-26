namespace Invoicing.API.Features.Invoices.CreateInvoices;

public sealed record CreateInvoicesCommandResponse(
    ICollection<SuccessfulInvoice> SuccessfulInvoices,
    ICollection<FailedInvoice> FailedInvoices
);

public sealed record SuccessfulInvoice(Guid InvoiceId, string ClientId);

public sealed record FailedInvoice(string ClientId, string Reason);