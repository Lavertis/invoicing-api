namespace Invoicing.API.Features.Invoices.Shared;

public sealed class InvoiceResponse
{
    public Guid Id { get; init; }
    public required string ClientId { get; init; }
    public int Year { get; init; }
    public int Month { get; init; }
    public decimal TotalValue => Items.Sum(item => item.Value);
    public required ICollection<InvoiceItemResponse> Items { get; init; }
}

public sealed class InvoiceItemResponse
{
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public decimal Value { get; init; }
    public bool IsSuspended { get; init; }
    public required string ServiceId { get; init; }
}