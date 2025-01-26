using FluentValidation.TestHelper;
using Invoicing.API.Features.Invoices.GetInvoices;

namespace Invoicing.Tests.Invoices.GetInvoices
{
    public class GetInvoicesQueryValidatorTests
    {
        private readonly GetInvoicesQueryValidator _validator;

        public GetInvoicesQueryValidatorTests()
        {
            _validator = new GetInvoicesQueryValidator();
        }

        [Theory]
        [InlineData(0, 2021, false)]
        [InlineData(13, 2021, false)]
        [InlineData(5, 1999, false)]
        [InlineData(5, 2021, true)]
        public void Validate_GetInvoicesQuery_ShouldValidateCorrectly(int month, int year, bool expectedIsValid)
        {
            // Arrange
            var query = new GetInvoicesQuery { Month = month, Year = year };

            // Act
            var result = _validator.TestValidate(query);

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