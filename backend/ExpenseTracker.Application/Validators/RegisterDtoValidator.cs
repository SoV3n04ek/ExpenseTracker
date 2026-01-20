using ExpenseTracker.Application.DTOs;
using FluentValidation;

namespace ExpenseTracker.Application.Validators
{
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        public RegisterDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(50).WithMessage("Name cannot exceed 50 characters");

            RuleFor(x => x.Email)
                .NotEmpty().EmailAddress()
                .MaximumLength(100);

            RuleFor(x => x.Password)
                .NotEmpty().MinimumLength(8);

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("Passwords do not match");
        }
    }
}