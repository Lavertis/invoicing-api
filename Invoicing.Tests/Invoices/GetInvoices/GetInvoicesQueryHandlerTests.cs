using Invoicing.API.Features.Invoices.GetInvoices;
using Invoicing.Domain.Entities;
using Shouldly;

namespace Invoicing.Tests.Invoices.GetInvoices;

public class GetInvoicesQueryHandlerTests : BaseTest
{
    private readonly GetInvoicesQueryHandler _handler;

    public GetInvoicesQueryHandlerTests()
    {
        _handler = new GetInvoicesQueryHandler(Context);
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
        result.Value.ShouldNotBeNull();
        result.Value.Records.Count.ShouldBe(1);
        result.Value.Records.First().Id.ShouldBe(invoice.Id);
        result.Value.Records.First().Items.Count.ShouldBe(1);
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
        result.Value.ShouldNotBeNull();
        result.Value.Records.ShouldBeEmpty();
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
        result.Value.ShouldNotBeNull();
        result.Value.Records.Count.ShouldBe(10);
        result.Value.TotalCount.ShouldBe(20);
        result.Value.Records.ShouldAllBe(record => record.Items.Count == 1);
    }
}