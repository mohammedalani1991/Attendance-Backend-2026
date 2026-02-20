using AttendanceWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace AttendanceWeb.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Extract Authorization header
        if (!context.HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Authorization header missing." });
            return;
        }

        var authHeaderValue = authHeader.ToString();
        if (!authHeaderValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Invalid authorization header format." });
            return;
        }

        var token = authHeaderValue.Substring("Bearer ".Length).Trim();
        if (string.IsNullOrEmpty(token))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Token is empty." });
            return;
        }

        // Validate token against database
        var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

        var apiToken = await dbContext.ApiTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token);

        if (apiToken == null)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Invalid token." });
            return;
        }

        // Check if token is expired
        if (apiToken.ExpiresAt < DateTime.UtcNow)
        {
            // Delete expired token
            dbContext.ApiTokens.Remove(apiToken);
            await dbContext.SaveChangesAsync();

            context.Result = new UnauthorizedObjectResult(new { message = "Token expired." });
            return;
        }

        // Check if user is a lecturer
        if (apiToken.User?.Role != Models.Domain.UserRole.Lecturer)
        {
            context.Result = new ForbidResult();
            return;
        }

        // Store userId in HttpContext for use in controllers
        context.HttpContext.Items["UserId"] = apiToken.UserId;
        context.HttpContext.Items["User"] = apiToken.User;
    }
}
