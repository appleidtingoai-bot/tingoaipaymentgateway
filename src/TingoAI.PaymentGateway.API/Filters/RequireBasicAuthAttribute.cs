using System;

namespace TingoAI.PaymentGateway.API.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class RequireBasicAuthAttribute : Attribute
    {
    }
}
