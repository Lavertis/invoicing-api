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
        [InlineData(0, 100.0, OperationType.StartService, false)]
        [InlineData(1, 10001.0, OperationType.StartService, false)]
        [InlineData(null, 100.0, OperationType.StartService, false)]
        [InlineData(1, null, OperationType.StartService, false)]
        [InlineData(null, null, OperationType.SuspendService, true)]
        [InlineData(1, 100.0, OperationType.SuspendService, false)]
        [InlineData(null, 100.0, OperationType.SuspendService, false)]
        [InlineData(1, null, OperationType.SuspendService, false)]
        [InlineData(null, null, OperationType.ResumeService, true)]
        [InlineData(1, 100.0, OperationType.ResumeService, false)]
        [InlineData(null, 100.0, OperationType.ResumeService, false)]
        [InlineData(1, null, OperationType.ResumeService, false)]
        [InlineData(null, null, OperationType.EndService, true)]
        [InlineData(1, 100.0, OperationType.EndService, false)]
        [InlineData(null, 100.0, OperationType.EndService, false)]
        [InlineData(1, null, OperationType.EndService, false)]
        public void Validate_CreateOperationCommand_ShouldValidateCorrectly(int? quantity, double? pricePerDay,
            OperationType type, bool expectedIsValid)
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