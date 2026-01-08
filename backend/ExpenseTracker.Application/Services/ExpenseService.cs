using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public ExpenseService(ApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        private int GetCurrentUserId() => int.Parse(_currentUserService.UserId ?? "0");

        public async Task<int> AddExpenseAsync(CreateExpenseDto dto)
        {
            var expense = new Expense
            {
                Description = dto.Description,
                Amount = dto.Amount,
                Date = dto.Date,
                CategoryId = dto.CategoryId,
                UserId = GetCurrentUserId()
            };

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();
            return expense.Id;
        }

        public async Task<IEnumerable<ExpenseDto>> GetAllExpensesAsync()
        {
            var currentUserId = GetCurrentUserId();

            return await _context.Expenses
                .Include(e => e.Category)
                .Where(e => e.UserId == currentUserId)
                .Select(e => new ExpenseDto(
                    e.Id,
                    e.Description,
                    e.Amount,
                    e.Date,
                    e.Category.Name
                ))
                .ToListAsync();
        }

        public async Task<ExpenseDto> GetExpenseByIdAsync(int id)
        {
            var currentUserId = GetCurrentUserId();

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

        public async Task UpdateExpenseAsync(int id, CreateExpenseDto dto)
        {
            var currentUser = GetCurrentUserId();
            var expense = await _context.Expenses.FirstOrDefaultAsync(e => 
                e.Id == id && e.UserId == currentUser);

            if (expense is null)
            {
                throw new KeyNotFoundException($"Expense with ID {id} not found.");
            }

            expense.Description = dto.Description;
            expense.Amount = dto.Amount;
            expense.Date = dto.Date;
            expense.CategoryId = dto.CategoryId;

            await _context.SaveChangesAsync();
        }
        public async Task DeleteExpenseAsync(int id)
        {
            var currentUser = GetCurrentUserId();
            //var expense = await _context.Expenses.FindAsync(id);
            var expense = await _context.Expenses.FirstOrDefaultAsync(e =>
                e.Id == id && e.UserId == currentUser);

            if (expense is null)
                throw new KeyNotFoundException($"Expense with ID {id} not found.");

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();
        }

        public async Task<ExpenseSummaryDto> GetSummaryAsync(DateTimeOffset startDate, DateTimeOffset endDate)
        {
            var userId = GetCurrentUserId();

            // Filter expenses by User AND the specific Date range
            var query = _context.Expenses
                .Where(e => e.UserId == userId &&
                    e.Date >= startDate &&
                    e.Date <= endDate);

            var totalAmount = await query.SumAsync(e => e.Amount);

            // Group by Category
            var categoryData = await query
                .GroupBy(e => e.Category.Name)
                .Select(g => new CategorySummaryDto
                {
                    CategoryName = g.Key,
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
    }
}
