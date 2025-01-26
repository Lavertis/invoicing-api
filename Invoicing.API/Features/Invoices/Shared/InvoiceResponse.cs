namespace Invoicing.API.Features.Invoices.Shared;

public sealed record InvoiceResponse(
    Guid Id,
    string ClientId,
    int Year,
    int Month,
    ICollection<InvoiceItemResponse> Items
)
{
    public decimal TotalValue { get; } = Items.Sum(item => item.Value);
}

public sealed record InvoiceItemResponse(
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Value,
    bool IsSuspended,
    string ServiceId
);