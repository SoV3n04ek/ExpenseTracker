using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Infrastructure.Persistence;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using ExpenseTracker.Application.Exceptions;
using ValidationException = ExpenseTracker.Application.Exceptions.ValidationException;

namespace ExpenseTracker.Application.Services
{
    public class ExpenseService : BaseService, IExpenseService
    {
        private readonly ApplicationDbContext _context;

        public ExpenseService(ApplicationDbContext context, ICurrentUserService currentUserService) 
            : base(currentUserService)
        {
            _context = context;
        }

        public async Task<int> AddExpenseAsync(CreateExpenseDto dto)
        {
            var userId = CurrentUserId;

            // Manual check for category existence as we moved it from async validator
            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists)
            {
                throw new ValidationException(new List<ValidationFailure> 
                { 
                    new ValidationFailure("CategoryId", "The selected category does not exist") 
                });
            }

            // Prevent duplicate entries (same amount, description, and category within 10 seconds)
            var tenSecondsAgo = DateTimeOffset.UtcNow.AddSeconds(-10);
            var isDuplicate = await _context.Expenses.AnyAsync(e =>
                e.UserId == userId &&
                e.Amount == dto.Amount &&
                e.Description == dto.Description &&
                e.CategoryId == dto.CategoryId &&
                e.Date >= tenSecondsAgo);

            if (isDuplicate)
            {
                throw new InvalidOperationException("A duplicate expense was detected. Please wait a moment.");
            }

            var expense = new Expense
            {
                Description = dto.Description,
                Amount = dto.Amount,
                Date = dto.Date.ToUniversalTime(),
                CategoryId = dto.CategoryId,
                UserId = CurrentUserId
            };

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();
            return expense.Id;
        }

        public async Task<PagedResponse<ExpenseDto>> GetPagedExpensesAsync(int pageNumber, int pageSize)
        {
            var userId = CurrentUserId;

            // 1. Create the base query
            var query = _context.Expenses
                .Include(e => e.Category)
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.Date); // Always sort for consistent pagination

            // 2. Count total items (for metadata)
            var totalCount = await query.CountAsync();

            // 3. Apply Pagination
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new ExpenseDto(
                    e.Id,
                    e.Description,
                    e.Amount,
                    e.Date,
                    e.Category.Name
                ))
                .ToListAsync();

            return new PagedResponse<ExpenseDto>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<ExpenseDto> GetExpenseByIdAsync(int id)
        {
            var currentUserId = CurrentUserId;

            var expense = await _context.Expenses
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => 
                    e.Id == id && e.UserId == currentUserId);

            if (expense is null)
            {
                throw new KeyNotFoundException($"Expense with id {id} not found.");
            }

            return new ExpenseDto(
                expense.Id,
                expense.Description,
                expense.Amount,
                expense.Date,
                expense.Category.Name);
        }

        public async Task UpdateExpenseAsync(int id, UpdateExpenseDto dto, string userId)
        {
            if (!int.TryParse(userId, out var parsedUserId))
            {
                throw new UnauthorizedAccessException("Invalid User ID.");
            }

            var expense = await _context.Expenses.FirstOrDefaultAsync(e => 
                e.Id == id && e.UserId == parsedUserId);

            if (expense is null)
            {
                throw new NotFoundException(nameof(Expense), id);
            }

            // Update only allowed fields
            expense.Description = dto.Description;
            expense.Amount = dto.Amount;
            expense.Date = dto.Date.ToUniversalTime();

            await _context.SaveChangesAsync();
        }

        public async Task DeleteExpenseAsync(int id, string userId)
        {
            if (!int.TryParse(userId, out var parsedUserId))
            {
                throw new UnauthorizedAccessException("Invalid User ID.");
            }

            // Use IgnoreQueryFilters to check if already deleted for idempotency
            var expense = await _context.Expenses
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e =>
                    e.Id == id && e.UserId == parsedUserId);

            if (expense is null)
            {
                throw new NotFoundException(nameof(Expense), id);
            }

            if (expense.IsDeleted) return; // Idempotency check

            expense.IsDeleted = true;
            expense.DeletedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<ExpenseSummaryDto> GetSummaryAsync(DateTimeOffset startDate, DateTimeOffset endDate)
        {
            var userId = CurrentUserId;

            // Filter expenses by User AND the specific Date range
            var startUtc = startDate.ToUniversalTime();
            var endUtc = endDate.ToUniversalTime();

            // Filter expenses using the UTC values
            var query = _context.Expenses
                .Where(e => e.UserId == userId &&
                            e.Date >= startUtc &&
                            e.Date <= endUtc);

            var totalAmount = await query.SumAsync(e => e.Amount);

            // Group by Category
            var categoryData = await query
                .GroupBy(e => new { e.Category.Id, e.Category.Name })
                .Select(g => new CategorySummaryDto
                {
                    CategoryId = g.Key.Id, // Now you can populate this!
                    CategoryName = g.Key.Name,
                    Amount = g.Sum(e => e.Amount),
                    Percentage = totalAmount > 0
                        ? (double)Math.Round(g.Sum(e => e.Amount) / totalAmount * 100, 2)
                        : 0
                })
                .ToListAsync();

            return new ExpenseSummaryDto
            {
                TotalAmount = totalAmount,
                Categories = categoryData
            };
        }

        public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync()
        {
            return await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto(c.Id, c.Name))
                .ToListAsync();
        }
    }
}
