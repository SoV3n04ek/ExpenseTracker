namespace ExpenseTracker.Domain.Entities
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // One category can have a collection of Expense 
        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    }
}
