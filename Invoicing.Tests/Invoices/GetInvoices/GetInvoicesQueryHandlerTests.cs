using Invoicing.API.Features.Invoices.GetInvoices;
using Invoicing.Domain.Entities;

namespace Invoicing.Tests.Invoices.GetInvoices;

public class GetInvoicesQueryHandlerTests : BaseTest
{
    private readonly GetInvoicesQueryHandler _handler;

    public GetInvoicesQueryHandlerTests()
    {
        _handler = new GetInvoicesQueryHandler(Context, Mapper);
    }

    [Fact]
    public async Task Handle_ShouldReturnInvoices_WhenInvoicesExist()
    {
        // Arrange
        const string clientId = "client1";
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            Month = 1,
            Year = 2023,
            CreatedAt = DateTime.UtcNow,
            Items =
            [
                new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
                    EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
                    Value = 100,
                    IsSuspended = false,
                    ServiceId = "service1"
                }
            ]
        };
        Context.Invoices.Add(invoice);
        await Context.SaveChangesAsync();

        var query = new GetInvoicesQuery
        {
            ClientId = clientId,
            Month = 1,
            Year = 2023,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Records);
        Assert.Equal(invoice.Id, result.Value.Records.First().Id);
        Assert.Single(result.Value.Records.First().Items);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoInvoicesExist()
    {
        // Arrange
        var query = new GetInvoicesQuery
        {
            ClientId = "client1",
            Month = 1,
            Year = 2023,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Records);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedInvoices_WhenMultipleInvoicesExist()
    {
        // Arrange
        const string clientId = "client1";
        for (var i = 0; i < 20; i++)
        {
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                Month = 1,
                Year = 2023,
                CreatedAt = DateTime.UtcNow,
                Items =
                [
                    new InvoiceItem
                    {
                        Id = Guid.NewGuid(),
                        StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
                        EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
                        Value = 100,
                        IsSuspended = false,
                        ServiceId = "service1"
                    }
                ]
            };
            Context.Invoices.Add(invoice);
        }

        await Context.SaveChangesAsync();

        var query = new GetInvoicesQuery
        {
            ClientId = clientId,
            Month = 1,
            Year = 2023,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result.Value);
        Assert.Equal(10, result.Value.Records.Count);
        Assert.Equal(20, result.Value.TotalCount);
        Assert.All(result.Value.Records, record => Assert.Single(record.Items));
    }
}