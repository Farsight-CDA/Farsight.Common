using Farsight.Common.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Farsight.Common;

public static class IHostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddApplication<TStartup>(this IHostApplicationBuilder builder)
        where TStartup : FarsightStartup
    {
        builder.Services.AddHostedService<TStartup>();
        FarsightCommonRegistry.Apply(builder);
        return builder;
    }
}
