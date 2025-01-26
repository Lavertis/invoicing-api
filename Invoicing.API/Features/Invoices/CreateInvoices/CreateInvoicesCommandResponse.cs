namespace Invoicing.API.Features.Invoices.CreateInvoices;

public record CreateInvoicesCommandResponse(
    ICollection<SuccessfulInvoice> SuccessfulInvoices,
    ICollection<FailedInvoice> FailedInvoices
);

public record SuccessfulInvoice(Guid InvoiceId, string ClientId);

public record FailedInvoice(string ClientId, string Reason);