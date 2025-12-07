using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using TingoAI.PaymentGateway.API.Filters;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace TingoAI.PaymentGateway.API.Swagger
{
    // Adds a Basic auth security requirement to Swagger operations that have [RequireBasicAuth]
    public class BasicAuthOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var hasAuthAttribute = false;

            // Check controller and method for the marker attribute
            var actionDescriptor = context.ApiDescription.ActionDescriptor as ControllerActionDescriptor;
            if (actionDescriptor != null)
            {
                var methodAttrs = actionDescriptor.MethodInfo.GetCustomAttributes(true);
                var controllerAttrs = actionDescriptor.ControllerTypeInfo.GetCustomAttributes(true);

                if (methodAttrs.Any(a => a.GetType() == typeof(RequireBasicAuthAttribute)) ||
                    controllerAttrs.Any(a => a.GetType() == typeof(RequireBasicAuthAttribute)))
                {
                    hasAuthAttribute = true;
                }
            }

            if (!hasAuthAttribute)
                return;

            operation.Security ??= new System.Collections.Generic.List<OpenApiSecurityRequirement>();
            var scheme = new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "basic" }
            };

            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [scheme] = new string[] { }
            });
        }
    }
}
