using ExpenseTracker.Application.DTOs;
using FluentValidation;

namespace ExpenseTracker.Application.Validators
{
    public class CreateExpenseDtoValidator : AbstractValidator<CreateExpenseDto>
    {
        public CreateExpenseDtoValidator()
        {
            RuleFor(dto => dto.Description)
                .NotEmpty().WithMessage("Description is required")
                .Must(d => d != null && d.Trim().Length > 0).WithMessage("Description cannot be whitespace only")
                .MaximumLength(100).WithMessage("Description cannot exceed 100 characters.");

            RuleFor(dto => dto.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than zero");

            RuleFor(dto => dto.Date)
                .LessThanOrEqualTo(dto => DateTimeOffset.UtcNow.AddDays(1))
                .WithMessage("Date cannot be in the future");

            RuleFor(dto => dto.CategoryId)
                .GreaterThan(0).WithMessage("Id must be greater than zero");
        }
    }
}