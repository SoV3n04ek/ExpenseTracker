using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace ExpenseTracker.API.Filters
{
    public class ValidateUserFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out _))
                {
                    context.Result = new UnauthorizedObjectResult(new { message = "Invalid or missing user identity." });
                    return;
                }
            }

            await next();
        }
    }
}
