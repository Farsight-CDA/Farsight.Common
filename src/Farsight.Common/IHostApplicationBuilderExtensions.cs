using Farsight.Common.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Farsight.Common;

/// <summary>
/// Extension methods for wiring Farsight registrations into a host application builder.
/// </summary>
public static class IHostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds startup orchestration and applies generated options and service registrations.
    /// </summary>
    /// <typeparam name="TStartup">The startup lifecycle service type.</typeparam>
    /// <param name="builder">The host builder to configure.</param>
    /// <returns>The same host builder for chaining.</returns>
    public static IHostApplicationBuilder AddApplication<TStartup>(this IHostApplicationBuilder builder)
        where TStartup : FarsightStartup
    {
        builder.Services.AddHostedService<TStartup>();
        builder.AddApplicationOptions();
        FarsightCommonRegistry.ApplyServices(builder);
        return builder;
    }

    /// <summary>
    /// Applies only generated options registrations.
    /// </summary>
    /// <param name="builder">The host builder to configure.</param>
    /// <returns>The same host builder for chaining.</returns>
    public static IHostApplicationBuilder AddApplicationOptions(this IHostApplicationBuilder builder)
    {
        FarsightCommonRegistry.ApplyOptions(builder);
        return builder;
    }
}
