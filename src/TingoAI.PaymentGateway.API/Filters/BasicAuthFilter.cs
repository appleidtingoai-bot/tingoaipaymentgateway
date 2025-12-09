using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TingoAI.PaymentGateway.API.Filters
{
    public class BasicAuthFilter : IAsyncActionFilter
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BasicAuthFilter> _logger;

        public BasicAuthFilter(IConfiguration configuration, ILogger<BasicAuthFilter> logger)
        {
            _configuration = configuration;
            _logger = logger;
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
            var user = _configuration["PaymentBasicAuth:Username"]?.Trim();
            var pass = _configuration["PaymentBasicAuth:Password"]?.Trim();

            var hasUser = !string.IsNullOrEmpty(user);
            var hasPass = !string.IsNullOrEmpty(pass);
            _logger?.LogDebug("BasicAuth config present: user={HasUser}, pass={HasPass}", hasUser, hasPass);

            if (hasUser && hasPass)
            {
                expectedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{pass}"));
            }
            else
            {
                expectedToken = _configuration["PaymentBasicAuth:Token"]?.Trim();
            }

            if (string.IsNullOrEmpty(expectedToken) || !string.Equals(token.Trim(), expectedToken.Trim(), StringComparison.Ordinal))
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
