using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Application.DTOs
{
    public record RegisterDto
    {
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        [MaxLength(64)]
        public string Password { get; init; } = string.Empty;
        public string ConfirmPassword { get; init; } = string.Empty;
    }
}
