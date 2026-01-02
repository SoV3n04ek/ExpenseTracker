using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;

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
        public async Task<ActionResult<IEnumerable<ExpenseDto>>> GetExpenses()
        {
            return Ok(await _expenseService.GetAllExpensesAsync());
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
        public async Task<IActionResult> UpdateResult(int id, CreateExpenseDto dto)
        {
            var validationResult = await _validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                throw new Application.Exceptions.ValidationException(validationResult.Errors);
            }

            await _expenseService.UpdateExpenseAsync(id, dto);
            return NoContent(); // 204 No content is standard for successful updates
        }

        // DELETE api/expenses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            await _expenseService.DeleteExpenseAsync(id);
            return NoContent();
        }

        // GET /summary
        [HttpGet("summary")]
        public async Task<ActionResult<ExpenseSummaryDto>> GetSummary(
            [FromQuery] int? month, [FromQuery] int? year)
        {
            return Ok(await _expenseService.GetSummaryAsync(month, year));
        }
    }
}
