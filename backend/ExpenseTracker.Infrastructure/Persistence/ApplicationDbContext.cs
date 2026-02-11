using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace ExpenseTracker.Infrastructure.Persistence
{
    public class ApplicationDbContext 
        : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Expense> Expenses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Groceries" },
                new Category { Id = 2, Name = "Leisure" },
                new Category { Id = 3, Name = "Electronics" },
                new Category { Id = 4, Name = "Utilities" },
                new Category { Id = 5, Name = "Clothing" },
                new Category { Id = 6, Name = "Health" },
                new Category { Id = 7, Name = "Others" }
            );

            modelBuilder.Entity<Category>()
                .Property(c => c.Id)
                .ValueGeneratedNever();

            modelBuilder.Entity<Expense>()
                .HasOne(e => e.User)
                .WithMany(u => u.Expenses)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Soft Delete
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(GetIsDeletedFilterExpression(entityType.ClrType));
                    modelBuilder.Entity(entityType.ClrType).HasIndex(nameof(ISoftDeletable.IsDeleted));
                }
            }
        }

        private static LambdaExpression GetIsDeletedFilterExpression(Type type)
        {
            var parameter = Expression.Parameter(type, "it");
            var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            var condition = Expression.Equal(property, Expression.Constant(false));
            return Expression.Lambda(condition, parameter);
        }
    }
}
