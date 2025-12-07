using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace TingoAI.PaymentGateway.API.Filters
{
    public class BasicAuthFilter : IAsyncActionFilter
    {
        private readonly IConfiguration _configuration;

        public BasicAuthFilter(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var req = context.HttpContext.Request;
            var headers = req.Headers;

            if (!headers.TryGetValue("Authorization", out var authValues))
            {
                Challenge(context);
                return;
            }

            var authHeader = authValues.FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                Challenge(context);
                return;
            }

            var token = authHeader.Substring(6).Trim();

            // Resolve expected token. First try Username/Password config, otherwise allow explicit token.
            string expectedToken = null;
            var user = _configuration["PaymentBasicAuth:Username"];
            var pass = _configuration["PaymentBasicAuth:Password"];
            if (!string.IsNullOrEmpty(user) && pass != null)
            {
                expectedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{pass}"));
            }
            else
            {
                expectedToken = _configuration["PaymentBasicAuth:Token"];
            }

            if (string.IsNullOrEmpty(expectedToken) || !string.Equals(token, expectedToken, StringComparison.Ordinal))
            {
                Challenge(context);
                return;
            }

            await next();
        }

        private static void Challenge(ActionExecutingContext context)
        {
            context.HttpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"TingoAI\"";
            context.Result = new UnauthorizedResult();
        }
    }
}
