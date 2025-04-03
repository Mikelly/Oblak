namespace Oblak.Middleware
{
    public class AuthenticationRedirectMiddleware
    {
        private readonly RequestDelegate _next; 
        public AuthenticationRedirectMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var user = context.User; 
            var allowedPaths = new[] { "/sign-in", };
             
            if (allowedPaths.Any(path => context.Request.Path.StartsWithSegments(path, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            } 

            if (user == null || !user.Identity.IsAuthenticated)
            {
                context.Response.Redirect("/sign-in");
                return;
            }

            await _next(context);
        }
    }
}
