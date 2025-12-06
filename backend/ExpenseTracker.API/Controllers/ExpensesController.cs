using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExpensesController : ControllerBase
    {
        private readonly IExpenseService _expenseService;

        public ExpensesController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        // POST api/expenses
        [HttpPost]
        public async Task<IActionResult> CreateExpense(CreateExpenseDto dto)
        {
            if (dto.Amount <= 0)
            {
                return BadRequest("Amount must be positive.");
            }

            int id = await _expenseService.AddExpenseAsync(dto);

            return CreatedAtAction(nameof(GetExpense), new { id = id }, dto);
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
            try
            {
                var expense = await _expenseService.GetExpenseByIdAsync(id);
                return Ok(expense);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        // PUT api/expenses/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateResult(int id, CreateExpenseDto dto)
        {
            try
            {
                await _expenseService.UpdateExpenseAsync(id, dto);
                return NoContent(); // 204 No content is standard for successful updates
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        // DELETE api/expenses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            await _expenseService.DeleteExpenseAsync(id);
            return NoContent();
        }
    }
}
