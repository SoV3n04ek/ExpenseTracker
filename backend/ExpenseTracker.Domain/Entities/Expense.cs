using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Domain.Entities
{
    public class Expense
    {
        public int Id { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTimeOffset Date { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

    }
}
