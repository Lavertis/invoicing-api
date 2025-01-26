using FluentValidation.TestHelper;
using Invoicing.API.Features.Operations.CreateOperation;
using Invoicing.Domain.Enums;

namespace Invoicing.Tests.Operations.CreateOperation
{
    public class CreateOperationCommandValidatorTests
    {
        private readonly CreateOperationCommandValidator _validator;

        public CreateOperationCommandValidatorTests()
        {
            _validator = new CreateOperationCommandValidator();
        }

        [Theory]
        [InlineData(1, 100.0, OperationType.StartService, true)]
        [InlineData(0, 100.0, OperationType.StartService, false)] // Invalid quantity range
        [InlineData(1, 10001.0, OperationType.StartService, false)] // Invalid price range
        [InlineData(null, 100.0, OperationType.StartService, false)] // Missing quantity
        [InlineData(1, null, OperationType.StartService, false)] // Missing price
        [InlineData(null, null, OperationType.SuspendService, true)]
        [InlineData(1, 100.0, OperationType.SuspendService, false)] // Unnecessary quantity and price
        [InlineData(null, 100.0, OperationType.SuspendService, false)] // Unnecessary price
        [InlineData(1, null, OperationType.SuspendService, false)] // Unnecessary quantity
        [InlineData(null, null, OperationType.ResumeService, true)]
        [InlineData(1, 100.0, OperationType.ResumeService, false)] // Unnecessary quantity and price
        [InlineData(null, 100.0, OperationType.ResumeService, false)] // Unnecessary price
        [InlineData(1, null, OperationType.ResumeService, false)] // Unnecessary quantity
        [InlineData(null, null, OperationType.EndService, true)]
        [InlineData(1, 100.0, OperationType.EndService, false)] // Unnecessary quantity and price
        [InlineData(null, 100.0, OperationType.EndService, false)] // Unnecessary price
        [InlineData(1, null, OperationType.EndService, false)] // Unnecessary quantity
        public void Validate_CreateOperationCommand_ShouldValidateCorrectly(
            int? quantity, double? pricePerDay, OperationType type, bool expectedIsValid
        )
        {
            // Arrange
            var command = new CreateOperationCommand
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