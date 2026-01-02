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
            // setup in-memory db
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                // every test gets a completely fresh, empty database.
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _mockUser = new Mock<ICurrentUserService>();

            // Mock the user as ID 1
            _mockUser.Setup(u => u.UserId).Returns("1");

            _service = new ExpenseService(_context, _mockUser.Object);
        }

        [Fact]
        public async Task GetSummaryAsync_ShouldAggregateDataCorrectly()
        {
            // Arrange 
            var category = new Category { Id = 1, Name = "Food" };
            var date = new DateTimeOffset(2025, 11, 15, 0, 0, 0, TimeSpan.Zero);
            _context.Expenses.AddRange(new List<Expense>
            {
                // same category, same month -> should be summed
                new() { Id = 1, Description = "Pizza", Amount = 20, Date = date, Category = category, UserId = 1},
                new() { Id = 2, Description = "Burger", Amount = 30, Date = date, Category = category, UserId = 1},
                // different month -> should be ignored
                new() { Id = 3, Description = "Old food", Amount = 100, Date = date.AddMonths(-1), Category = category, UserId = 1}
            });

            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetSummaryAsync(11, 2025);

            // Assert
            Assert.Equal(50, result.TotalAmount);
            Assert.Single(result.Categories);
            Assert.Equal("Food", result.Categories[0].CategoryName);
            Assert.Equal(100, result.Categories[0].Percentage);
        }
    }
}
