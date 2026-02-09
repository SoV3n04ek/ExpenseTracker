namespace ExpenseTracker.Application.Constants;

public static class AuthConstants
{
    public const string PasswordRegex = @"^(?=.*[0-9])(?=.*[!@#$%^&*(),.?Value"":{}|<>]).*$";
    public const string PasswordRegexErrorMessage = "Password must contain at least one digit and one special character.";
}