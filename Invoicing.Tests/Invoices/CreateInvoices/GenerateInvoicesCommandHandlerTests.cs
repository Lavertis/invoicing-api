using Invoicing.API.Features.Invoices.GenerateInvoices;
using Invoicing.Domain.Entities;
using Invoicing.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Invoicing.Tests.Invoices.CreateInvoices;

public class GenerateInvoicesCommandHandlerTests : BaseTest
{
    private readonly GenerateInvoicesCommandHandler _handler;

    public GenerateInvoicesCommandHandlerTests()
    {
        _handler = new GenerateInvoicesCommandHandler(Context);
    }

    [Fact]
    public async Task CreatesInvoice_WhenProvisionIsFinished()
    {
        // Arrange
        const int year = 2025;
        const int month = 2;
        var command = new GenerateInvoicesCommand { Month = 2, Year = year };

        // Seed data
        const string clientId = "client1";
        var serviceProvision = new ServiceProvision
            { ClientId = clientId, ServiceId = "service1", PricePerDay = 10, Quantity = 2 };
        var operations = new List<ServiceOperation>
        {
            new()
            {
                ServiceProvision = serviceProvision, Date = new DateOnly(year, month, 1),
                Type = ServiceOperationType.Start
            },
            new()
            {
                ServiceProvision = serviceProvision, Date = new DateOnly(year, month, 3),
                Type = ServiceOperationType.Suspend
            },
            new()
            {
                ServiceProvision = serviceProvision, Date = new DateOnly(year, month, 5),
                Type = ServiceOperationType.Resume
            },
            new()
            {
                ServiceProvision = serviceProvision, Date = new DateOnly(year, month, 20),
                Type = ServiceOperationType.End
            }
        };
        Context.ServiceOperations.AddRange(operations);
        await Context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.StatusCode.ShouldBe(StatusCodes.Status200OK);
        result.Data.ShouldNotBeNull();
        result.Data.SuccessfulInvoices.Count.ShouldBe(1);
        result.Data.FailedInvoices.ShouldBeEmpty();

        var invoiceCount = await Context.Invoices.CountAsync(CancellationToken.None);
        invoiceCount.ShouldBe(1);

        var invoice = await Context.Invoices.Include(i => i.Items).FirstAsync(CancellationToken.None);
        invoice.ClientId.ShouldBe(clientId);
        invoice.Month.ShouldBe(month);
        invoice.Year.ShouldBe(year);
        invoice.Items.Count.ShouldBe(2);

        var firstItem = invoice.Items[0];
        firstItem.ServiceId.ShouldBe(serviceProvision.ServiceId);
        firstItem.StartDate.ShouldBe(operations[0].Date);
        firstItem.EndDate.ShouldBe(operations[1].Date);
        firstItem.Value.ShouldBe(40);

        var secondItem = invoice.Items[1];
        secondItem.ServiceId.ShouldBe(serviceProvision.ServiceId);
        secondItem.StartDate.ShouldBe(operations[2].Date);
        secondItem.EndDate.ShouldBe(operations[3].Date);
        secondItem.Value.ShouldBe(300);
    }

    [Fact]
    public async Task ReturnsFailure_WhenProvisionIsNotFinished()
    {
        // Arrange
        var command = new GenerateInvoicesCommand { Month = 1, Year = 2025 };

        // Seed data
        const string clientId = "client1";
        var serviceProvision = new ServiceProvision
            { ClientId = clientId, ServiceId = "service1", PricePerDay = 10, Quantity = 1 };
        var operations = new List<ServiceOperation>
        {
            new()
            {
                ServiceProvision = serviceProvision, Date = new DateOnly(2025, 1, 1), Type = ServiceOperationType.Start
            }
            // Missing EndService operation
        };
        Context.ServiceOperations.AddRange(operations);
        await Context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.StatusCode.ShouldBe(StatusCodes.Status200OK);
        result.Data.ShouldNotBeNull();
        result.Data.SuccessfulInvoices.ShouldBeEmpty();
        result.Data.FailedInvoices.Count.ShouldBe(1);
        Context.Invoices.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReturnsFailedInvoice_WhenInvoiceAndNonInvoicedOperationsExist()
    {
        // Arrange
        var command = new GenerateInvoicesCommand { Month = 1, Year = 2025 };

        // Seed data
        const string clientId = "client1";
        var serviceProvision = new ServiceProvision
            { ClientId = clientId, ServiceId = "service1", PricePerDay = 10, Quantity = 1 };
        var operations = new List<ServiceOperation>
        {
            new()
            {
                ServiceProvision = serviceProvision, Date = new DateOnly(2025, 1, 1), Type = ServiceOperationType.Start
            },
            new()
            {
                ServiceProvision = serviceProvision, Date = new DateOnly(2025, 1, 31), Type = ServiceOperationType.End
            }
        };
        Context.ServiceOperations.AddRange(operations);
        var invoice = new Invoice { ClientId = clientId, Month = 1, Year = 2025 };
        Context.Invoices.Add(invoice);
        await Context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.StatusCode.ShouldBe(StatusCodes.Status200OK);
        result.Data.ShouldNotBeNull();
        result.Data.SuccessfulInvoices.ShouldBeEmpty();
        result.Data.FailedInvoices.Count.ShouldBe(1);
        (await Context.Invoices.CountAsync(CancellationToken.None)).ShouldBe(1);
    }

    [Fact]
    public async Task CreatesSeparateInvoices_WhenMultipleClientsHaveFinishedOperations()
    {
        // Arrange
        const int year = 2025;
        const int month = 2;
        var command = new GenerateInvoicesCommand { Month = month, Year = year };

        // Seed data for client1
        const string client1Id = "client1";
        var serviceProvision1 = new ServiceProvision
            { ClientId = client1Id, ServiceId = "service1", PricePerDay = 10, Quantity = 2 };
        var operations1 = new List<ServiceOperation>
        {
            new()
            {
                ServiceProvision = serviceProvision1, Date = new DateOnly(year, month, 1),
                Type = ServiceOperationType.Start
            },
            new()
            {
                ServiceProvision = serviceProvision1, Date = new DateOnly(year, month, 3),
                Type = ServiceOperationType.Suspend
            },
            new()
            {
                ServiceProvision = serviceProvision1, Date = new DateOnly(year, month, 5),
                Type = ServiceOperationType.Resume
            },
            new()
            {
                ServiceProvision = serviceProvision1, Date = new DateOnly(year, month, 20),
                Type = ServiceOperationType.End
            }
        };

        // Seed data for client2
        const string client2Id = "client2";
        var serviceProvision2 = new ServiceProvision
            { ClientId = client2Id, ServiceId = "service2", PricePerDay = 15, Quantity = 1 };
        var operations2 = new List<ServiceOperation>
        {
            new()
            {
                ServiceProvision = serviceProvision2, Date = new DateOnly(year, month, 2),
                Type = ServiceOperationType.Start
            },
            new()
            {
                ServiceProvision = serviceProvision2, Date = new DateOnly(year, month, 10),
                Type = ServiceOperationType.End
            }
        };

        Context.ServiceOperations.AddRange(operations1);
        Context.ServiceOperations.AddRange(operations2);
        await Context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.StatusCode.ShouldBe(StatusCodes.Status200OK);
        result.Data.ShouldNotBeNull();
        result.Data.SuccessfulInvoices.Count.ShouldBe(2);
        result.Data.FailedInvoices.ShouldBeEmpty();

        var invoices = await Context.Invoices.Include(i => i.Items).ToListAsync(CancellationToken.None);
        invoices.Count.ShouldBe(2);

        var invoice1 = invoices.First(i => i.ClientId == client1Id);
        invoice1.Month.ShouldBe(month);
        invoice1.Year.ShouldBe(year);
        invoice1.Items.Count.ShouldBe(2);

        var firstItem1 = invoice1.Items[0];
        firstItem1.ServiceId.ShouldBe(serviceProvision1.ServiceId);
        firstItem1.StartDate.ShouldBe(operations1[0].Date);
        firstItem1.EndDate.ShouldBe(operations1[1].Date);
        firstItem1.Value.ShouldBe(40);

        var secondItem1 = invoice1.Items[1];
        secondItem1.ServiceId.ShouldBe(serviceProvision1.ServiceId);
        secondItem1.StartDate.ShouldBe(operations1[2].Date);
        secondItem1.EndDate.ShouldBe(operations1[3].Date);
        secondItem1.Value.ShouldBe(300);

        var invoice2 = invoices.First(i => i.ClientId == client2Id);
        invoice2.Month.ShouldBe(month);
        invoice2.Year.ShouldBe(year);
        invoice2.Items.Count.ShouldBe(1);

        var firstItem2 = invoice2.Items[0];
        firstItem2.ServiceId.ShouldBe(serviceProvision2.ServiceId);
        firstItem2.StartDate.ShouldBe(operations2[0].Date);
        firstItem2.EndDate.ShouldBe(operations2[1].Date);
        firstItem2.Value.ShouldBe(120);
    }

    [Fact]
    public async Task DoesNotCreateInvoice_WhenInvoiceExistsAndAllOperationsAreInvoiced()
    {
        // Arrange
        var command = new GenerateInvoicesCommand { Month = 1, Year = 2025 };

        // Seed data
        const string clientId = "client1";
        var serviceProvision = new ServiceProvision
            { ClientId = clientId, ServiceId = "service1", PricePerDay = 10, Quantity = 1 };
        var operations = new List<ServiceOperation>
        {
            new()
            {
                ServiceProvision = serviceProvision, Date = new DateOnly(2025, 1, 1), Type = ServiceOperationType.Start
            },
            new()
            {
                ServiceProvision = serviceProvision, Date = new DateOnly(2025, 1, 31), Type = ServiceOperationType.End
            }
        };
        Context.ServiceOperations.AddRange(operations);
        var invoice = new Invoice { ClientId = clientId, Month = 1, Year = 2025 };
        invoice.Items.Add(new InvoiceItem
        {
            ServiceId = serviceProvision.ServiceId,
            StartDate = operations[0].Date,
            EndDate = operations[1].Date,
            Value = 300
        });
        Context.Invoices.Add(invoice);
        await Context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.StatusCode.ShouldBe(StatusCodes.Status200OK);
        result.Data.ShouldNotBeNull();
        result.Data.SuccessfulInvoices.ShouldBeEmpty();
        result.Data.FailedInvoices.ShouldBeEmpty();
        (await Context.Invoices.CountAsync(CancellationToken.None)).ShouldBe(1);
    }
}