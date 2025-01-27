using FluentAssertions;
using Invoicing.API.Features.Invoices.GenerateInvoices;
using Invoicing.Domain.Entities;
using Invoicing.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

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
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Value.Should().NotBeNull();
        result.Value.SuccessfulInvoices.Should().HaveCount(1);
        result.Value.FailedInvoices.Should().BeEmpty();

        var invoiceCount = await Context.Invoices.CountAsync(cancellationToken);
        invoiceCount.Should().Be(1);

        var invoice = await Context.Invoices.Include(i => i.Items).FirstAsync(cancellationToken);
        invoice.ClientId.Should().Be(clientId);
        invoice.Month.Should().Be(month);
        invoice.Year.Should().Be(year);
        invoice.Items.Should().HaveCount(2);

        var firstItem = invoice.Items[0];
        firstItem.ServiceId.Should().Be(serviceProvision.ServiceId);
        firstItem.StartDate.Should().Be(operations[0].Date);
        firstItem.EndDate.Should().Be(operations[1].Date);
        firstItem.Value.Should().Be(40);

        var secondItem = invoice.Items[1];
        secondItem.ServiceId.Should().Be(serviceProvision.ServiceId);
        secondItem.StartDate.Should().Be(operations[2].Date);
        secondItem.EndDate.Should().Be(operations[3].Date);
        secondItem.Value.Should().Be(300);
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
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Value.Should().NotBeNull();
        result.Value.SuccessfulInvoices.Should().BeEmpty();
        result.Value.FailedInvoices.Should().HaveCount(1);
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
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Value.Should().NotBeNull();
        result.Value.SuccessfulInvoices.Should().BeEmpty();
        result.Value.FailedInvoices.Should().HaveCount(1);
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
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Value.Should().NotBeNull();
        result.Value.SuccessfulInvoices.Should().HaveCount(2);
        result.Value.FailedInvoices.Should().BeEmpty();

        var invoiceCount = await Context.Invoices.CountAsync(cancellationToken);
        invoiceCount.Should().Be(2);

        var invoices = await Context.Invoices.Include(i => i.Items).ToListAsync(cancellationToken);

        var invoice1 = invoices.First(i => i.ClientId == client1Id);
        invoice1.Month.Should().Be(month);
        invoice1.Year.Should().Be(year);
        invoice1.Items.Should().HaveCount(2);

        var firstItem1 = invoice1.Items[0];
        firstItem1.ServiceId.Should().Be(serviceProvision1.ServiceId);
        firstItem1.StartDate.Should().Be(operations1[0].Date);
        firstItem1.EndDate.Should().Be(operations1[1].Date);
        firstItem1.Value.Should().Be(40);

        var secondItem1 = invoice1.Items[1];
        secondItem1.ServiceId.Should().Be(serviceProvision1.ServiceId);
        secondItem1.StartDate.Should().Be(operations1[2].Date);
        secondItem1.EndDate.Should().Be(operations1[3].Date);
        secondItem1.Value.Should().Be(300);

        var invoice2 = invoices.First(i => i.ClientId == client2Id);
        invoice2.Month.Should().Be(month);
        invoice2.Year.Should().Be(year);
        invoice2.Items.Should().HaveCount(1);

        var firstItem2 = invoice2.Items[0];
        firstItem2.ServiceId.Should().Be(serviceProvision2.ServiceId);
        firstItem2.StartDate.Should().Be(operations2[0].Date);
        firstItem2.EndDate.Should().Be(operations2[1].Date);
        firstItem2.Value.Should().Be(120);
    }
}