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
    public async Task Handle_ShouldCreateInvoice_ForFinishedProvision()
    {
        // Arrange
        const int year = 2023;
        const int month = 2;
        var command = new GenerateInvoicesCommand { Month = 2, Year = year };
        var cancellationToken = CancellationToken.None;

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
        await Context.SaveChangesAsync(cancellationToken);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.StatusCode.ShouldBe(StatusCodes.Status200OK);
        result.Value.ShouldNotBeNull();
        result.Value.SuccessfulInvoices.Count.ShouldBe(1);
        result.Value.FailedInvoices.ShouldBeEmpty();

        var invoiceCount = await Context.Invoices.CountAsync(cancellationToken);
        invoiceCount.ShouldBe(1);

        var invoice = await Context.Invoices.Include(i => i.Items).FirstAsync(cancellationToken);
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
    public async Task Handle_ShouldReturnFailure_WhenProvisionNotFinished()
    {
        // Arrange
        var command = new GenerateInvoicesCommand { Month = 1, Year = 2023 };
        var cancellationToken = CancellationToken.None;

        // Seed data
        const string clientId = "client1";
        var serviceProvision = new ServiceProvision
            { ClientId = clientId, ServiceId = "service1", PricePerDay = 10, Quantity = 1 };
        var operations = new List<ServiceOperation>
        {
            new()
            {
                ServiceProvision = serviceProvision, Date = new DateOnly(2023, 1, 1), Type = ServiceOperationType.Start
            }
            // Missing EndService operation
        };
        Context.ServiceOperations.AddRange(operations);
        await Context.SaveChangesAsync(cancellationToken);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.StatusCode.ShouldBe(StatusCodes.Status200OK);
        result.Value.ShouldNotBeNull();
        result.Value.SuccessfulInvoices.ShouldBeEmpty();
        result.Value.FailedInvoices.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailedInvoice_WhenInvoiceAndNonInvoicedOperationsExist()
    {
        // Arrange
        var command = new GenerateInvoicesCommand { Month = 1, Year = 2023 };
        var cancellationToken = CancellationToken.None;

        // Seed data
        const string clientId = "client1";
        var serviceProvision = new ServiceProvision
            { ClientId = clientId, ServiceId = "service1", PricePerDay = 10, Quantity = 1 };
        var operations = new List<ServiceOperation>
        {
            new()
            {
                ServiceProvision = serviceProvision, Date = new DateOnly(2023, 1, 1), Type = ServiceOperationType.Start
            },
            new()
            {
                ServiceProvision = serviceProvision, Date = new DateOnly(2023, 1, 31), Type = ServiceOperationType.End
            }
        };
        Context.ServiceOperations.AddRange(operations);
        var invoice = new Invoice { ClientId = clientId, Month = 1, Year = 2023 };
        Context.Invoices.Add(invoice);
        await Context.SaveChangesAsync(cancellationToken);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.StatusCode.ShouldBe(StatusCodes.Status200OK);
        result.Value.ShouldNotBeNull();
        result.Value.SuccessfulInvoices.ShouldBeEmpty();
        result.Value.FailedInvoices.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_ShouldCreateInvoices_ForMultipleClients()
    {
        // Arrange
        const int year = 2023;
        const int month = 2;
        var command = new GenerateInvoicesCommand { Month = month, Year = year };
        var cancellationToken = CancellationToken.None;

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
        await Context.SaveChangesAsync(cancellationToken);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.StatusCode.ShouldBe(StatusCodes.Status200OK);
        result.Value.ShouldNotBeNull();
        result.Value.SuccessfulInvoices.Count.ShouldBe(2);
        result.Value.FailedInvoices.ShouldBeEmpty();

        var invoiceCount = await Context.Invoices.CountAsync(cancellationToken);
        invoiceCount.ShouldBe(2);

        var invoices = await Context.Invoices.Include(i => i.Items).ToListAsync(cancellationToken);

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
}