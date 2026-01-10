using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Application.DTOs
{
    public record CreateExpenseDto
    {
        public CreateExpenseDto(string description, decimal amount, DateTimeOffset date, int categoryId)
        {
            Description = description;
            Amount = amount;
            Date = date;
            CategoryId = categoryId;
        }

        public string Description { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public DateTimeOffset Date { get; init; }
        public int CategoryId { get; init; }
    }

    public record ExpenseDto (
        int Id,
        string Description,
        decimal Amount,
        DateTimeOffset Date,
        string CategoryName);
}
