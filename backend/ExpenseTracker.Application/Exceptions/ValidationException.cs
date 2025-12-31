namespace ExpenseTracker.Application.Exceptions
{
    public class ValidationException : Exception
    {
        public IDictionary<string, string[]> Errors { get; }

        public ValidationException(string message) 
            : base(message) 
        {
            Errors = new Dictionary<string, string[]>();
        }

        public ValidationException(List<FluentValidation.Results.ValidationFailure> failures) 
            : base("One or more validation have occured.")
        {
            Errors = failures
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
        }
    }
}
