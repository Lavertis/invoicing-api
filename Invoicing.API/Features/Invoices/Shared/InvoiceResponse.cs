namespace Invoicing.API.Features.Invoices.Shared;

public sealed record InvoiceResponse(
    Guid Id,
    string ClientId,
    int Year,
    int Month,
    ICollection<InvoiceItemResponse> Items
);

public sealed record InvoiceItemResponse(
    DateOnly StartDate,
    DateOnly EndDate,
    int Value,
    bool IsSuspended,
    string ServiceId
);