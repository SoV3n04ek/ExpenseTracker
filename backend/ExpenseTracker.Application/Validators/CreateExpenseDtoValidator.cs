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
            RuleFor(dto => dto.Description)
                .NotEmpty().WithMessage("Description is required")
                .MaximumLength(200).WithMessage("Description cannot exceed 200 characters");

            RuleFor(dto => dto.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than zero");

            RuleFor(dto => dto.CategoryId)
                .GreaterThan(0).WithMessage("Id must be greater than zero");
        }

        public CreateExpenseDtoValidator(IServiceScopeFactory factory) : this()
        {
            _scopeFactory = factory;

            RuleFor(dto => dto.CategoryId)
                .MustAsync(BeExistingCategory).WithMessage("The selected category does not exist");

        }

        private async Task<bool> BeExistingCategory(int categoryId, CancellationToken cancellationToken)
        {
            if (_scopeFactory == null) return true;

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