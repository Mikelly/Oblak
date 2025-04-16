using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace Oblak
{ 
    public static class SecurityExtensions
    {  
        public static IServiceCollection ConfigureCustomApplicationCookie(this IServiceCollection services)
        {
            return services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = "/greska/pristup-zabranjen";
                options.LoginPath = "/";

                //options.Events = new CookieAuthenticationEvents
                //{
                //    OnValidatePrincipal = async context =>
                //    {
                //        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
                //        var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<IdentityUser>>();

                //        if (context.Principal == null)
                //            return;

                //        var user = await userManager.GetUserAsync(context.Principal);
                //        if (user != null)
                //        {
                //            var securityStamp = await userManager.GetSecurityStampAsync(user);
                //            var securityStampClaim = context.Principal.FindFirst("AspNet.Identity.SecurityStamp")?.Value;

                //            if (securityStamp != securityStampClaim)
                //            {
                //                context.RejectPrincipal();
                //                await signInManager.SignOutAsync();
                //                await context.HttpContext.SignOutAsync();
                //                context.HttpContext.Session.Clear();

                //                var request = context.HttpContext.Request;
                //                bool isAjax = request.Headers["X-Requested-With"] == "XMLHttpRequest";

                //                if (isAjax)
                //                {
                //                    context.Response.StatusCode = 401;
                //                    context.Response.ContentType = "application/json";
                //                    await context.Response.WriteAsync("{\"error\":\"Neautorizovan pristup! Ponovite proces login-a.\"}");
                //                }
                //                else
                //                {
                //                    context.Response.Redirect("/sign-in");
                //                }
                //            }
                //        }
                //    },
                //    OnRedirectToLogin = context =>
                //    {
                //        var request = context.HttpContext.Request;
                //        bool isAjax = request.Headers["X-Requested-With"] == "XMLHttpRequest";

                //        if (isAjax)
                //        {
                //            context.Response.StatusCode = 401;
                //            context.Response.ContentType = "application/json";
                //            return context.Response.WriteAsync("{\"error\":\"Neautorizovan pristup! Ponovite proces login-a.\"}");
                //        }
                //        else
                //        {
                //            context.Response.Redirect("/sign-in");
                //        }

                //        return Task.CompletedTask;
                //    }
                //};

            });
        }
    }

}
