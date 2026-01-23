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
                _logger.LogError(ex, $"An unhandled exception occured during request processing. \n{ex.Message}");
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var statusCode = HttpStatusCode.InternalServerError;
            object errorResponse = new { message = "An unexpected error occurred." };

            switch (exception)
            {
                case ValidationException valEx:
                    statusCode = HttpStatusCode.BadRequest;
                    errorResponse = new
                    {
                        statusCode = (int)statusCode,
                        title = "One or more validation errors occurred.",
                        errors = valEx.Errors
                    };
                    break;

                case UnauthorizedAccessException authEx:
                    statusCode = HttpStatusCode.Unauthorized;
                    errorResponse = new { message = authEx.Message };
                    break;

                case KeyNotFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    errorResponse = new { message = "The requested resource was not found." };
                    break;

                case InvalidOperationException invEx:
                    statusCode = HttpStatusCode.Conflict;
                    errorResponse = new { message = invEx.Message };
                    break;

                default:
                    if (exception.Message.Contains("Registration failed"))
                    {
                        statusCode = HttpStatusCode.BadRequest;
                        errorResponse = new { message = exception.Message };
                    }
                    break;
            }

            context.Response.StatusCode = (int)statusCode;

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            return context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, options));
        }
    }
}
