using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Application.DTOs
{
    public record LoginDto
    {
        public string Email { get; init; } = string.Empty;
        [MaxLength(64)]
        public string Password { get; init; } = string.Empty;
    }
}
