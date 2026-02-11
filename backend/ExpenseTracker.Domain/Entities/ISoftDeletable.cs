namespace ExpenseTracker.Domain.Entities
{
    public interface ISoftDeletable
    {
        bool IsDeleted { get; set; }
        DateTime? DeletedAtUtc { get; set; }
    }
}
