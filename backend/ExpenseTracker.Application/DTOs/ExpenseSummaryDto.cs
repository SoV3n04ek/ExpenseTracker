namespace ExpenseTracker.Application.DTOs
{
    public class ExpenseSummaryDto
    {
        public decimal TotalAmount { get; set; }
        public List<CategorySummaryDto> Categories { get; set; } = new();
    }
}