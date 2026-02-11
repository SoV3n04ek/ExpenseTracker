namespace ExpenseTracker.Application.DTOs
{
    public record UpdateExpenseDto
    {
        public UpdateExpenseDto(string description, decimal amount, DateTimeOffset date)
        {
            Description = description;
            Amount = amount;
            Date = date;
        }

        public string Description { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public DateTimeOffset Date { get; init; }
    }
}
