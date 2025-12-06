using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Application.DTOs
{
    public record CreateExpenseDto(
            [Required] string Description,
            decimal Amount,
            DateTimeOffset Date,
            int CategoryId);

    public record ExpenseDto (
        int Id,
        string Description,
        decimal Amount,
        DateTimeOffset Date,
        string CategoryName);
}
