using ExpenseTracker.Application.Exceptions;
using System.Net;
using System.Text.Json;

namespace ExpenseTracker.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occured during request processing.");
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            // Default to 500 internal server error
            var statusCode = HttpStatusCode.InternalServerError;
            object errorResponse = new { message = "An unexcpected error occured." };

            if (exception is ValidationException validationException)
            {
                statusCode = HttpStatusCode.BadRequest;
                errorResponse = new
                {
                    statusCode = (int)statusCode,
                    title = "One or more validation error occured.",
                    errors = validationException.Errors
                };
            }
            else if (exception is KeyNotFoundException)
            {
                statusCode = HttpStatusCode.NotFound;
                errorResponse = new { message = "The requested resource was not found." };
            }
            else
            {
                Console.WriteLine($"exception: " + exception.Message);
            }

            context.Response.StatusCode = (int)statusCode;

            return context.Response.WriteAsync(
                JsonSerializer.Serialize(
                    errorResponse, 
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }
    }
}
