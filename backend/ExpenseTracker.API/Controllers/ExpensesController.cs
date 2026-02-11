using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ExpenseTracker.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ExpensesController : ControllerBase
    {
        private readonly IExpenseService _expenseService;
        private readonly IValidator<CreateExpenseDto> _validator;

        public ExpensesController(IExpenseService expenseService, IValidator<CreateExpenseDto> validator)
        {
            _expenseService = expenseService;
            _validator = validator;
        }

        // POST api/expenses
        [HttpPost]
        public async Task<IActionResult> CreateExpense(CreateExpenseDto dto)
        {
            var validationResult = await _validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                throw new Application.Exceptions.ValidationException(validationResult.Errors);
            }

            int id = await _expenseService.AddExpenseAsync(dto);

            return CreatedAtAction(nameof(GetExpense), new { id }, dto);
        }

        // GET api/expenses
        [HttpGet]
        public async Task<ActionResult<PagedResponse<ExpenseDto>>> GetExpenses(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            // Prevent huge requests by capping pageSize
            if (pageSize > 50) pageSize = 50;

            var result = await _expenseService.GetPagedExpensesAsync(pageNumber, pageSize);
            return Ok(result);
        }

        // GET api/expenses/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ExpenseDto>> GetExpense(int id)
        {
            var expense = await _expenseService.GetExpenseByIdAsync(id);
            return Ok(expense);
        }

        // PUT api/expenses/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExpense(int id, UpdateExpenseDto dto)
        {
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _expenseService.UpdateExpenseAsync(id, dto, userId);
            return NoContent(); // 204 No content is standard for successful updates
        }

        // DELETE api/expenses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _expenseService.DeleteExpenseAsync(id, userId);
            return NoContent();
        }

        // GET /summary
        [HttpGet("summary")]
        public async Task<ActionResult<ExpenseSummaryDto>> GetSummary(
            [FromQuery] DateTimeOffset? startDate, 
            [FromQuery] DateTimeOffset? endDate)
        {
            var start = startDate ?? DateTimeOffset.UtcNow.AddDays(-30);
            var end = endDate ?? DateTimeOffset.UtcNow;

            if (start > end)
            {
                var ex = new Application.Exceptions.ValidationException("Invalid date range");
                ex.Errors.Add("DateRange", new[] { "Start date must be before end date" });
                throw ex;
            }

            return Ok(await _expenseService.GetSummaryAsync(start, end));
        }

        // GET /categories
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            return Ok(await _expenseService.GetCategoriesAsync());
        }
    }
}
