namespace ExpenseTracker.Application.DTOs
{
    public class CategorySummaryDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public double Percentage { get; set; }
    }
}