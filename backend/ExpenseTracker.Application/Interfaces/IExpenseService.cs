using ExpenseTracker.Application.DTOs;

namespace ExpenseTracker.Application.Interfaces
{
    public interface IExpenseService
    {
        Task<ExpenseDto> GetExpenseByIdAsync(int id);
        Task<PagedResponse<ExpenseDto>> GetPagedExpensesAsync(int pageNumber, int pageSize);
        Task<int> AddExpenseAsync(CreateExpenseDto dto);
        Task UpdateExpenseAsync(int id, CreateExpenseDto dto);
        Task DeleteExpenseAsync(int id);
        Task<ExpenseSummaryDto> GetSummaryAsync(DateTimeOffset startDate, DateTimeOffset endDate);
        Task<IEnumerable<CategoryDto>> GetCategoriesAsync();
    }
}
