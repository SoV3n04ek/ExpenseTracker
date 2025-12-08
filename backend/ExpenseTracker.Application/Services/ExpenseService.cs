using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Exceptions;
using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly ApplicationDbContext _context;

        public ExpenseService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> AddExpenseAsync(CreateExpenseDto dto)
        {
            if (dto.Amount <= 0)
            {
                throw new ValidationException($"Expense amount cannot be <= 0");
            }

            var expense = new Expense
            {
                Description = dto.Description,
                Amount = dto.Amount,
                Date = dto.Date,
                CategoryId = dto.CategoryId
            };

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();
            return expense.Id;
        }

        public async Task<IEnumerable<ExpenseDto>> GetAllExpensesAsync()
        {
            return await _context.Expenses
                .Include(e => e.Category)
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
            var expense = await _context.Expenses
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.Id == id);

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
            var expense = await _context.Expenses.FindAsync(id);

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
            var expense = await _context.Expenses.FindAsync(id);

            if (expense != null)
            {
                _context.Expenses.Remove(expense);
                await _context.SaveChangesAsync();
            }
        }

    }
}
