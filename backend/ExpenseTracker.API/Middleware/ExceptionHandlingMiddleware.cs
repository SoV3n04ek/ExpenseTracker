using ExpenseTracker.Application.Exceptions;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context; // Required for LogContext

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
            // Get the id
            var traceId = httpContext.TraceIdentifier;
            
            using (LogContext.PushProperty("TraceId", traceId))
            {
                try
                {
                    await _next(httpContext);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception Occurred: {Message}", ex.Message);
                    await HandleExceptionAsync(httpContext, ex, traceId);
                }
            }
            
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception, string traceId)
        {
            if (context.Response.HasStarted)
            {
                return;
            }

            var statusCode = exception switch
            {
                NotFoundException or KeyNotFoundException => HttpStatusCode.NotFound,
                ValidationException => HttpStatusCode.BadRequest,
                UnauthorizedAccessException => HttpStatusCode.Unauthorized,
                InvalidOperationException => HttpStatusCode.Conflict,
                _ => HttpStatusCode.InternalServerError
            };

            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/problem+json";

            var problemDetails = new ProblemDetails
            {
                Status = (int)statusCode,
                Type = $"https://httpstatuses.com/{(int)statusCode}",
                Title = exception.GetType().Name,
                Detail = exception.Message,
                Instance = context.Request.Path
            };

            // Adds the TraceId to the JSON response so the frontend sees it
            problemDetails.Extensions["traceId"] = traceId;

            // Special handling for Validation Errors to match RFC format
            if (exception is ValidationException valEx)
            {
                problemDetails.Extensions["errors"] = valEx.Errors;
            }

            var options = new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            await context.Response.WriteAsJsonAsync(problemDetails, options, contentType: "application/problem+json");
        }
    }
}
