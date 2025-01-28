using Invoicing.API.Features.ServiceOperations.CreateServiceOperation;
using Invoicing.Domain.Entities;
using Invoicing.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace Invoicing.Tests.ServiceOperations.CreateServiceOperation;

public class CreateServiceOperationCommandHandlerTests : BaseTest
{
    private readonly CreateServiceOperationCommandHandler _handler;

    public CreateServiceOperationCommandHandlerTests()
    {
        _handler = new CreateServiceOperationCommandHandler(Context);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenDateIsBeforeLastServiceOperationDate()
    {
        // Arrange
        const string clientId = "client1";
        const string serviceId = "service1";
        var lastOperation = new ServiceOperation
        {
            Id = Guid.NewGuid(),
            ServiceProvision = new ServiceProvision
            {
                ClientId = clientId,
                ServiceId = serviceId,
                Quantity = 10,
                PricePerDay = 100
            },
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Type = ServiceOperationType.Start
        };
        Context.ServiceOperations.Add(lastOperation);
        await Context.SaveChangesAsync();

        var command = new CreateServiceOperationCommand
        {
            ServiceId = serviceId,
            ClientId = clientId,
            Quantity = 10,
            PricePerDay = 100,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Type = ServiceOperationType.Start
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        result.IsError.ShouldBeTrue();
    }

    [Theory]
    [InlineData(ServiceOperationType.Start, ServiceOperationType.Suspend, true)]
    [InlineData(ServiceOperationType.Start, ServiceOperationType.End, true)]
    [InlineData(ServiceOperationType.Start, ServiceOperationType.Start, false)]
    [InlineData(ServiceOperationType.Start, ServiceOperationType.Resume, false)]
    [InlineData(ServiceOperationType.Suspend, ServiceOperationType.Resume, true)]
    [InlineData(ServiceOperationType.Suspend, ServiceOperationType.End, false)]
    [InlineData(ServiceOperationType.Suspend, ServiceOperationType.Start, false)]
    [InlineData(ServiceOperationType.Suspend, ServiceOperationType.Suspend, false)]
    [InlineData(ServiceOperationType.Resume, ServiceOperationType.End, true)]
    [InlineData(ServiceOperationType.Resume, ServiceOperationType.Start, false)]
    [InlineData(ServiceOperationType.Resume, ServiceOperationType.Suspend, true)]
    [InlineData(ServiceOperationType.Resume, ServiceOperationType.Resume, false)]
    [InlineData(ServiceOperationType.End, ServiceOperationType.Start, true)]
    [InlineData(ServiceOperationType.End, ServiceOperationType.Resume, false)]
    [InlineData(ServiceOperationType.End, ServiceOperationType.Suspend, false)]
    [InlineData(ServiceOperationType.End, ServiceOperationType.End, false)]
    [InlineData(null, ServiceOperationType.Start, true)]
    [InlineData(null, ServiceOperationType.Suspend, false)]
    [InlineData(null, ServiceOperationType.Resume, false)]
    [InlineData(null, ServiceOperationType.End, false)]
    public async Task ValidatesOperationTransitionCorrectly(
        ServiceOperationType? lastOperationType,
        ServiceOperationType nextOperationType,
        bool isValid)
    {
        // Arrange
        const string clientId = "client1";
        const string serviceId = "service1";

        if (lastOperationType != null)
        {
            var lastOperation = new ServiceOperation
            {
                Id = Guid.NewGuid(),
                ServiceProvision = new ServiceProvision
                {
                    ClientId = clientId,
                    ServiceId = serviceId,
                    Quantity = 10,
                    PricePerDay = 100
                },
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                Type = lastOperationType.Value
            };
            Context.ServiceOperations.Add(lastOperation);
            await Context.SaveChangesAsync();
        }

        var command = new CreateServiceOperationCommand
        {
            ServiceId = serviceId,
            ClientId = clientId,
            Quantity = nextOperationType == ServiceOperationType.Start ? 10 : null,
            PricePerDay = nextOperationType == ServiceOperationType.Start ? 100 : null,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Type = nextOperationType
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        if (isValid)
        {
            result.StatusCode.ShouldBe(StatusCodes.Status201Created);
            result.Data.ShouldNotBeNull();
            result.IsSuccess.ShouldBeTrue();
        }
        else
        {
            result.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
            result.IsError.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task CreatesServiceOperation_ForValidData()
    {
        // Arrange
        const string clientId = "client1";
        const string serviceId = "service1";
        var lastOperation = new ServiceOperation
        {
            Id = Guid.NewGuid(),
            ServiceProvision = new ServiceProvision
            {
                ClientId = clientId,
                ServiceId = serviceId,
                Quantity = 10,
                PricePerDay = 100
            },
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Type = ServiceOperationType.Start
        };
        Context.ServiceOperations.Add(lastOperation);
        await Context.SaveChangesAsync();

        var command = new CreateServiceOperationCommand
        {
            ServiceId = serviceId,
            ClientId = clientId,
            Quantity = 10,
            PricePerDay = 100,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Type = ServiceOperationType.Suspend
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.StatusCode.ShouldBe(StatusCodes.Status201Created);
        result.Data.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        Context.ServiceOperations.Count().ShouldBe(2);
    }
}