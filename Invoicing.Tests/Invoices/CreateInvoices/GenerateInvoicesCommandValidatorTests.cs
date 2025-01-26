using FluentValidation.TestHelper;
using Invoicing.API.Features.Invoices.GenerateInvoices;

namespace Invoicing.Tests.Invoices.CreateInvoices
{
    public class GenerateInvoicesCommandValidatorTests
    {
        private readonly GenerateInvoicesCommandValidator _validator;

        public GenerateInvoicesCommandValidatorTests()
        {
            _validator = new GenerateInvoicesCommandValidator();
        }

        [Theory]
        [InlineData(0, 2021, false)]
        [InlineData(13, 2021, false)]
        [InlineData(5, 1999, false)]
        [InlineData(5, 2021, true)]
        public void Validate_CreateInvoicesCommand_ShouldValidateCorrectly(int month, int year, bool expectedIsValid)
        {
            // Arrange
            var command = new GenerateInvoicesCommand { Month = month, Year = year };

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