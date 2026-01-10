using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Validators;
using FluentValidation.TestHelper;

namespace ExpenseTracker.UnitTests
{
    public class CreateExpenseDtoValidatorTests
    {
        private readonly CreateExpenseDtoValidator _validator = new();

        [Fact]
        public void Should_Have_Error_When_Amount_Is_Negative()
        {
            // Arrange
            var model = new CreateExpenseDto("Description", -10.5m, DateTimeOffset.UtcNow, 1);

            // Act
            var result = _validator.TestValidate(model);
            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Amount)
                .WithErrorMessage("Amount must be greater than zero");
        }

        [Fact]
        public void Should_Have_Error_When_Description_Is_Empty()
        {
            var model = new CreateExpenseDto("", 10.5m, DateTimeOffset.UtcNow, 1);
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Description);
        }
    }
}
