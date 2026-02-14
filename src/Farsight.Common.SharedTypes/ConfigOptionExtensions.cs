using Microsoft.Extensions.Hosting;

namespace Farsight.Common;

public static class IHostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddApplication(this IHostApplicationBuilder builder)
    {
        FarsightCommonRegistry.Apply(builder);
        return builder;
    }
}
