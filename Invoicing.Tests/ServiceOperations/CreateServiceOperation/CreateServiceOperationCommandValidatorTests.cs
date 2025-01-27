using FluentValidation.TestHelper;
using Invoicing.API.Features.ServiceOperations.CreateServiceOperation;
using Invoicing.Domain.Enums;

namespace Invoicing.Tests.ServiceOperations.CreateServiceOperation
{
    public class CreateServiceOperationCommandValidatorTests
    {
        private readonly CreateServiceOperationCommandValidator _validator;

        public CreateServiceOperationCommandValidatorTests()
        {
            _validator = new CreateServiceOperationCommandValidator();
        }

        [Theory]
        [InlineData(1, 100.0, ServiceOperationType.Start, true)]
        [InlineData(0, 100.0, ServiceOperationType.Start, false)] // Invalid quantity range
        [InlineData(1, 10001.0, ServiceOperationType.Start, false)] // Invalid price range
        [InlineData(null, 100.0, ServiceOperationType.Start, false)] // Missing quantity
        [InlineData(1, null, ServiceOperationType.Start, false)] // Missing price
        [InlineData(null, null, ServiceOperationType.Suspend, true)]
        [InlineData(1, 100.0, ServiceOperationType.Suspend, false)] // Unnecessary quantity and price
        [InlineData(null, 100.0, ServiceOperationType.Suspend, false)] // Unnecessary price
        [InlineData(1, null, ServiceOperationType.Suspend, false)] // Unnecessary quantity
        [InlineData(null, null, ServiceOperationType.Resume, true)]
        [InlineData(1, 100.0, ServiceOperationType.Resume, false)] // Unnecessary quantity and price
        [InlineData(null, 100.0, ServiceOperationType.Resume, false)] // Unnecessary price
        [InlineData(1, null, ServiceOperationType.Resume, false)] // Unnecessary quantity
        [InlineData(null, null, ServiceOperationType.End, true)]
        [InlineData(1, 100.0, ServiceOperationType.End, false)] // Unnecessary quantity and price
        [InlineData(null, 100.0, ServiceOperationType.End, false)] // Unnecessary price
        [InlineData(1, null, ServiceOperationType.End, false)] // Unnecessary quantity
        public void Validate_CreateOperationCommand_ShouldValidateCorrectly(
            int? quantity, double? pricePerDay, ServiceOperationType type, bool expectedIsValid
        )
        {
            // Arrange
            var command = new CreateServiceOperationCommand
            {
                Quantity = quantity,
                PricePerDay = pricePerDay != null ? (decimal)pricePerDay : null,
                Type = type,
                ServiceId = "service1",
                ClientId = "client1"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            if (expectedIsValid)
            {
                result.ShouldNotHaveAnyValidationErrors();
            }
            else
            {
                result.ShouldHaveAnyValidationError();
            }
        }
    }
}