using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.RestApi.Factories;
using Nop.Plugin.Misc.RestApi.Services;

namespace Nop.Plugin.Misc.RestApi.Infrastructure;

/// <summary>
/// Represents object for the configuring services and middleware on application startup
/// </summary>
public class NopStartup : INopStartup
{
    /// <summary>
    /// Add and configure any of the middleware
    /// </summary>
    /// <param name="services">Collection of service descriptors</param>
    /// <param name="configuration">Configuration of the application</param>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ApiTokenService>();
        services.AddScoped<ApiModelFactory>();
        services.AddScoped<BackOfficeModelFactory>();
    }

    /// <summary>
    /// Configure the using of added middleware
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public void Configure(IApplicationBuilder application)
    {
        application.UseMiddleware<ApiAuthenticationMiddleware>();
    }

    /// <summary>
    /// Gets order of this startup configuration implementation:
    /// after error handlers (0), before the themes middleware (100) which would create a guest customer
    /// </summary>
    public int Order => 50;
}
