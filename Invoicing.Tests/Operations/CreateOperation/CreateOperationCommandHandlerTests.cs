using FluentAssertions;
using Invoicing.API.Features.Operations.CreateOperation;
using Invoicing.Domain.Entities;
using Invoicing.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Invoicing.Tests.Operations.CreateOperation;

public class CreateOperationCommandHandlerTests : BaseTest
{
    private readonly CreateOperationCommandHandler _handler;

    public CreateOperationCommandHandlerTests()
    {
        _handler = new CreateOperationCommandHandler(Context);
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenDateIsBeforeLastOperationDate()
    {
        // Arrange
        const string clientId = "client1";
        const string serviceId = "service1";
        var lastOperation = new ServiceProvisionOperation
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
            Type = OperationType.StartService
        };
        Context.ServiceProvisionOperations.Add(lastOperation);
        await Context.SaveChangesAsync();

        var command = new CreateOperationCommand
        {
            ServiceId = serviceId,
            ClientId = clientId,
            Quantity = 10,
            PricePerDay = 100,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Type = OperationType.StartService
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        result.IsError.Should().BeTrue();
    }

    [Theory]
    [InlineData(OperationType.StartService, OperationType.SuspendService, true)]
    [InlineData(OperationType.StartService, OperationType.EndService, true)]
    [InlineData(OperationType.StartService, OperationType.StartService, false)]
    [InlineData(OperationType.StartService, OperationType.ResumeService, false)]
    [InlineData(OperationType.SuspendService, OperationType.ResumeService, true)]
    [InlineData(OperationType.SuspendService, OperationType.EndService, false)]
    [InlineData(OperationType.SuspendService, OperationType.StartService, false)]
    [InlineData(OperationType.SuspendService, OperationType.SuspendService, false)]
    [InlineData(OperationType.ResumeService, OperationType.EndService, true)]
    [InlineData(OperationType.ResumeService, OperationType.StartService, false)]
    [InlineData(OperationType.ResumeService, OperationType.SuspendService, true)]
    [InlineData(OperationType.ResumeService, OperationType.ResumeService, false)]
    [InlineData(OperationType.EndService, OperationType.StartService, true)]
    [InlineData(OperationType.EndService, OperationType.ResumeService, false)]
    [InlineData(OperationType.EndService, OperationType.SuspendService, false)]
    [InlineData(OperationType.EndService, OperationType.EndService, false)]
    [InlineData(null, OperationType.StartService, true)]
    [InlineData(null, OperationType.SuspendService, false)]
    [InlineData(null, OperationType.ResumeService, false)]
    [InlineData(null, OperationType.EndService, false)]
    public async Task Handle_ShouldValidateOperationType(OperationType? lastOperationType,
        OperationType nextOperationType, bool isValid)
    {
        // Arrange
        const string clientId = "client1";
        const string serviceId = "service1";

        if (lastOperationType != null)
        {
            var lastOperation = new ServiceProvisionOperation
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
            Context.ServiceProvisionOperations.Add(lastOperation);
            await Context.SaveChangesAsync();
        }

        var command = new CreateOperationCommand
        {
            ServiceId = serviceId,
            ClientId = clientId,
            Quantity = nextOperationType == OperationType.StartService ? 10 : null,
            PricePerDay = nextOperationType == OperationType.StartService ? 100 : null,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Type = nextOperationType
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        if (isValid)
        {
            result.StatusCode.Should().Be(StatusCodes.Status201Created);
            result.Value.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
        }
        else
        {
            result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            result.IsError.Should().BeTrue();
        }
    }
}