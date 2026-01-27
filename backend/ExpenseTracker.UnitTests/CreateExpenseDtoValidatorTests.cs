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

        [Fact]
        public void Validator_ShouldNotHaveError_WhenDataIsValid()
        {
            // Arrange
            var model = new CreateExpenseDto(
                new string('A', 11), // Description
                10m,                  // Amount
                DateTimeOffset.UtcNow,// Date
                1                     // CategoryId
            );
            // Act
            var result = _validator.TestValidate(model);

            // Assert 
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-50)]
        public void Validator_ShouldHaveError_WhenAmountIsZeroOrNegative(decimal invalidAmount)
        {
            // Arrange
            var model = new CreateExpenseDto(
                "Valid Description",
                invalidAmount,
                DateTimeOffset.UtcNow,
                1);

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Amount)
                .WithErrorMessage("Amount must be greater than zero");
        }

        [Fact]
        public void Validator_ShouldHaveError_WhenDescriptionIsTooLong()
        {
            // Arrange
            var model = new CreateExpenseDto
            (
                new string('A', 101), // Max is 100
                10,
                DateTimeOffset.UtcNow,
                1
            );

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Description)
                  .WithErrorMessage("Description cannot exceed 100 characters.");
        }

        [Fact]
        public void Validator_ShouldHaveError_WhenDescriptionIsJustWhitespace()
        {
            // Arrange
            var model = new CreateExpenseDto("   ", 10.5m, DateTimeOffset.UtcNow, 1);

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Description)
                  .WithErrorMessage("Description cannot be whitespace only");
        }

        [Fact]
        public void Validator_ShouldHaveError_WhenDateIsTooFarInFuture()
        {
            // Arrange
            var model = new CreateExpenseDto("Description", 10.5m, DateTimeOffset.UtcNow.AddYears(100), 1);

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Date)
                  .WithErrorMessage("Date cannot be in the future");
        }
    }
}
