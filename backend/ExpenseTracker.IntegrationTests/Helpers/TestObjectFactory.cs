using ExpenseTracker.Application.DTOs;
using System;

namespace ExpenseTracker.IntegrationTests.Helpers
{
    public static class TestObjectFactory
    {
        public static string GetUniqueEmail() => $"user_{Guid.NewGuid()}@example.com";

        public static string GetUniqueExpenseName(string prefix = "Expense") => 
            $"{prefix}_{Guid.NewGuid().ToString()[..8]}";

        /// <summary>
        /// Returns a constant valid password that satisfies all constraints:
        /// 8-64 chars, 1 Upper, 1 Lower, 1 Digit, 1 Special.
        /// </summary>
        public static string GetSecurePassword() => "Pass123!_Admin";

        public static RegisterDto GetRegisterDto(string? email = null)
        {
            var validEmail = email ?? GetUniqueEmail();
            var password = GetSecurePassword();
            
            return new RegisterDto
            {
                Name = "Test User",
                Email = validEmail,
                Password = password,
                ConfirmPassword = password
            };
        }

        public static LoginDto GetLoginDto(string email, string? password = null)
        {
            return new LoginDto
            {
                Email = email,
                Password = password ?? GetSecurePassword()
            };
        }

        public static CreateExpenseDto GetCreateExpenseDto(
            string? description = null, 
            decimal amount = 100.00m, 
            int categoryId = 1)
        {
            return new CreateExpenseDto(
                description ?? GetUniqueExpenseName(),
                amount,
                DateTimeOffset.UtcNow,
                categoryId
            );
        }
    }
}
