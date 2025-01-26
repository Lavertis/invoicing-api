using Invoicing.API.Features.Invoices.GenerateInvoices;
using Invoicing.Domain.Entities;
using Invoicing.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Invoicing.Tests.Invoices.CreateInvoices;

public class GenerateInvoicesCommandHandlerTests : BaseTest
{
    private readonly GenerateInvoicesCommandHandler _handler;

    public GenerateInvoicesCommandHandlerTests()
    {
        _handler = new GenerateInvoicesCommandHandler(Context);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccessResult_WhenInvoicesAreCreatedSuccessfully()
    {
        // Arrange
        var command = new GenerateInvoicesCommand { Month = 1, Year = 2023 };
        var cancellationToken = CancellationToken.None;

        // Seed data
        const string clientId = "client1";
        var serviceProvision = new ServiceProvision
            { ClientId = clientId, ServiceId = "service1", PricePerDay = 10, Quantity = 1 };
        var operations = new List<ServiceProvisionOperation>
        {
            new()
            {
                ServiceProvision = serviceProvision, Date = new DateOnly(2023, 1, 1), Type = OperationType.StartService
            },
            new()
            {
                ServiceProvision = serviceProvision, Date = new DateOnly(2023, 1, 31), Type = OperationType.EndService
            }
        };
        Context.ServiceProvisionOperations.AddRange(operations);
        await Context.SaveChangesAsync(cancellationToken);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.SuccessfulInvoices);
        Assert.Empty(result.Value.FailedInvoices);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailedResult_WhenInvoiceCreationFails()
    {
        // Arrange
        var command = new GenerateInvoicesCommand { Month = 1, Year = 2023 };
        var cancellationToken = CancellationToken.None;

        // Seed data
        const string clientId = "client1";
        var serviceProvision = new ServiceProvision
            { ClientId = clientId, ServiceId = "service1", PricePerDay = 10, Quantity = 1 };
        var operations = new List<ServiceProvisionOperation>
        {
            new()
            {
                ServiceProvision = serviceProvision, Date = new DateOnly(2023, 1, 1), Type = OperationType.StartService
            }
            // Missing EndService operation
        };
        Context.ServiceProvisionOperations.AddRange(operations);
        await Context.SaveChangesAsync(cancellationToken);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.SuccessfulInvoices);
        Assert.Single(result.Value.FailedInvoices);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailedInvoice_WhenInvoiceAlreadyExistsAndOperationsAreNotInvoiced()
    {
        // Arrange
        var command = new GenerateInvoicesCommand { Month = 1, Year = 2023 };
        var cancellationToken = CancellationToken.None;

        // Seed data
        const string clientId = "client1";
        var serviceProvision = new ServiceProvision
            { ClientId = clientId, ServiceId = "service1", PricePerDay = 10, Quantity = 1 };
        var operations = new List<ServiceProvisionOperation>
        {
            new()
            {
                ServiceProvision = serviceProvision, Date = new DateOnly(2023, 1, 1), Type = OperationType.StartService
            },
            new()
            {
                ServiceProvision = serviceProvision, Date = new DateOnly(2023, 1, 31), Type = OperationType.EndService
            }
        };
        Context.ServiceProvisionOperations.AddRange(operations);
        var invoice = new Invoice { ClientId = clientId, Month = 1, Year = 2023 };
        Context.Invoices.Add(invoice);
        await Context.SaveChangesAsync(cancellationToken);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.SuccessfulInvoices);
        Assert.Single(result.Value.FailedInvoices);
    }
}