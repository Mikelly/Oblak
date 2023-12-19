using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Oblak.Models.Api;

namespace Oblak.Filters
{
    public class LoginResultFilterAttribute : ResultFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {   
            if(context.Result is ObjectResult result)
            {
                var loginResult = result.Value as LoginDto;
                var cookie = context.HttpContext.Request.Cookies[".AspNetCore.Identity.Application"];
                loginResult.auth = cookie;
                result.Value = loginResult;
            }
        }
        public override void OnResultExecuted(ResultExecutedContext context)
        {
            
        }

    }
}
