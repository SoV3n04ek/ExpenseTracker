using ExpenseTracker.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpenseTracker.Application.Interfaces
{
    public interface IExpenseService
    {
        Task<ExpenseDto> GetExpenseByIdAsync(int id);
        Task<IEnumerable<ExpenseDto>> GetAllExpensesAsync();
        Task<int> AddExpenseAsync(CreateExpenseDto dto);
        Task UpdateExpenseAsync(int id, CreateExpenseDto dto);
        Task DeleteExpenseAsync(int id);
    }
}
