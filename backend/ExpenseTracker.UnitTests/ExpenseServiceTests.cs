using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Application.Services;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ExpenseTracker.UnitTests
{
    public class ExpenseServiceTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ICurrentUserService> _mockUser;
        private readonly ExpenseService _service;

        public ExpenseServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _mockUser = new Mock<ICurrentUserService>();
            _mockUser.Setup(u => u.UserId).Returns("1");

            _service = new ExpenseService(_context, _mockUser.Object);
        }

        [Fact]
        public async Task GetSummaryAsync_ShouldAggregateRangeCorrectly()
        {
            // Arrange 
            var category = new Category { Id = 1, Name = "Food" };
            var start = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var end = new DateTimeOffset(2025, 1, 31, 23, 59, 59, TimeSpan.Zero);

            _context.Expenses.AddRange(new List<Expense>
            {
                // Inside Range
                new() { Id = 1, Description = "Pizza", Amount = 20, Date = start.AddDays(5), Category = category, UserId = 1},
                new() { Id = 2, Description = "Burger", Amount = 30, Date = start.AddDays(10), Category = category, UserId = 1},
                // Outside Range (Before)
                new() { Id = 3, Description = "Old food", Amount = 100, Date = start.AddDays(-5), Category = category, UserId = 1}
            });

            await _context.SaveChangesAsync();

            // Act - Passing the Date Range
            var result = await _service.GetSummaryAsync(start, end);

            // Assert
            Assert.Equal(50, result.TotalAmount);
            Assert.Single(result.Categories);
            Assert.Equal(100, result.Categories[0].Percentage);
        }

        [Fact]
        public async Task GetSummaryAsync_ShouldRespectUserIsolation()
        {
            // Arrange
            var category = new Category { Id = 1, Name = "Food" };
            var start = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var end = new DateTimeOffset(2025, 1, 31, 0, 0, 0, TimeSpan.Zero);

            _context.Expenses.AddRange(new List<Expense>
            {
                new() { Id = 1, Description = "My Expense", Amount = 50, Date = start.AddDays(1), Category = category, UserId = 1 },
                new() { Id = 2, Description = "Evil Hacker", Amount = 9999, Date = start.AddDays(1), Category = category, UserId = 2 }
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetSummaryAsync(start, end);

            // Assert
            Assert.Equal(50, result.TotalAmount);
        }

        [Fact]
        public async Task GetSummaryAsync_WhenNoExpensesExist_ReturnsEmptySummary()
        {
            // Arrange
            var start = DateTimeOffset.UtcNow.AddDays(-7);
            var end = DateTimeOffset.UtcNow;

            // Act
            var result = await _service.GetSummaryAsync(start, end);

            // Assert
            Assert.Equal(0, result.TotalAmount);
            Assert.Empty(result.Categories);
        }
    }
}