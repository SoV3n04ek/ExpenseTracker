using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTracker.Application.Validators
{
    public class CreateExpenseDtoValidator : AbstractValidator<CreateExpenseDto>
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public CreateExpenseDtoValidator()
        {
        }

        public CreateExpenseDtoValidator(IServiceScopeFactory factory)
        {
            _scopeFactory = factory;

            // Rule 1: Description Validation
            RuleFor(dto => dto.Description)
                .NotEmpty().WithMessage("Description is required")
                .MaximumLength(200).WithMessage("Description cannot exceed 200 characters");

            // Rule 2: Amount Validation
            RuleFor(dto => dto.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than zero");

            // Rule 3: Category Existense Check (Database Validation)
            RuleFor(dto => dto.CategoryId)
                .GreaterThan(0).WithMessage("Id must be greater than zero")
                .MustAsync(BeExistingCategory).WithMessage("The selected category does not exist");

            RuleFor(dto => dto.CategoryId)
                .MustAsync(BeExistingCategory).WithMessage("The selected category does not exist");

            RuleFor(dto => dto.Description)
                .NotEmpty().WithMessage("Description is required")
                .MaximumLength(200).WithMessage("Description cannot exceed 200 characters");

            RuleFor(dto => dto.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than zero");
        }

        private async Task<bool> BeExistingCategory(int categoryId, CancellationToken cancellationToken)
        {
            using (IServiceScope scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                if (categoryId <= 0)
                    return false;

                return await dbContext.Categories.AnyAsync(c => c.Id == categoryId, cancellationToken);
            }
        }
    }
}