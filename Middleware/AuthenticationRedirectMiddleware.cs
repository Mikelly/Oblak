using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Oblak.Data;

namespace Oblak.Middleware;

public class AuthenticationRedirectMiddleware
{
    private readonly RequestDelegate _next; 
    public AuthenticationRedirectMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
    {
        var path = context.Request.Path;
        var user = context.User; 
        var allowedPaths = new[] { "/sign-in", };

        if (allowedPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var isApi = path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) || context.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (user == null || !user.Identity?.IsAuthenticated == true)
        {
            if (isApi)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"error\":\"Neautorizovan pristup! Ponovite proces login-a.\"}");
            }
            else
            {
                context.Response.Redirect("/sign-in");
            }
            return;
        }

        var userName = user.Identity?.Name;

        if (!string.IsNullOrEmpty(userName))
        {
            var appUser = await dbContext
                .Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserName == userName);

            if (appUser != null &&
                appUser.LockoutEnabled &&
                appUser.LockoutEnd.HasValue &&
                appUser.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                await context.SignOutAsync(IdentityConstants.ApplicationScheme);

                if (isApi)
                {
                    context.Response.StatusCode = StatusCodes.Status423Locked;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\":\"Nalog je zaključan. Kontaktirajte administratora.\"}");
                }
                else
                {
                    context.Response.Redirect("/sign-in");
                }
                return;
            }
        }

        await _next(context);
    }
}

