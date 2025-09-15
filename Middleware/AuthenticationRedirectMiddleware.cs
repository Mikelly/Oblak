namespace Oblak.Middleware;

public class AuthenticationRedirectMiddleware
{
    private readonly RequestDelegate _next; 
    public AuthenticationRedirectMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;
        var user = context.User; 
        var allowedPaths = new[] { "/sign-in", };
         
        if (allowedPaths.Any(path => context.Request.Path.StartsWithSegments(path, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var isApi = path.StartsWithSegments("/api") || context.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (user == null || !user.Identity.IsAuthenticated)
        {
            if (isApi)
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"error\":\"Neautorizovan pristup! Ponovite proces login-a.\"}");
            }
            else
            {
                context.Response.Redirect("/sign-in");
            }
            return;
        }

        await _next(context);
    }
}
