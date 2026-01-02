using ExpenseTracker.Application.DTOs;

namespace ExpenseTracker.Application.Interfaces
{
    public interface IExpenseService
    {
        Task<ExpenseDto> GetExpenseByIdAsync(int id);
        Task<IEnumerable<ExpenseDto>> GetAllExpensesAsync();
        Task<int> AddExpenseAsync(CreateExpenseDto dto);
        Task UpdateExpenseAsync(int id, CreateExpenseDto dto);
        Task DeleteExpenseAsync(int id);
        Task<ExpenseSummaryDto> GetSummaryAsync(int? month, int? year);
    }
}
