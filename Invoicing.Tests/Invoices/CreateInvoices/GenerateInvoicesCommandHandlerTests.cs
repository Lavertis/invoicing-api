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
}