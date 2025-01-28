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
        [InlineData(5, 2021, true)] // Valid data
        [InlineData(0, 2021, false)] // Invalid month (less than 1)
        [InlineData(13, 2021, false)] // Invalid month (greater than 12)
        [InlineData(5, 1999, false)] // Invalid year (less than 2000)
        public void ValidatesCorrectly(int month, int year, bool expectedIsValid)
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