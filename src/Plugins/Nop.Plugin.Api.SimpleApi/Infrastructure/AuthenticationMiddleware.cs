using Microsoft.AspNetCore.Http;
using Nop.Core;
using System.Threading.Tasks;
using Nop.Services.Customers;

namespace Nop.Plugin.Api.SimpleApi.Infrastructure
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IWorkContext workContext, ICustomerService customerService, SimpleApiSettings apiSettings)
        {
            // Only apply to our API routes
            if (!context.Request.Path.StartsWithSegments("/api/simple"))
            {
                await _next(context);
                return;
            }

            // Allow access for administrators
            var customer = await workContext.GetCurrentCustomerAsync();
            if (customer != null && await customerService.IsAdminAsync(customer))
            {
                await _next(context);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(apiSettings.ApiKey) || !context.Request.Headers.TryGetValue("X-API-KEY", out var apiKeyHeader) || apiKeyHeader != apiSettings.ApiKey)
            {
                context.Response.StatusCode = 401; // Unauthorized
                await context.Response.WriteAsync("Invalid API Key");
                return;
            }

            await _next(context);
        }
    }
}
